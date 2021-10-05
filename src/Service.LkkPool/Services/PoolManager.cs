using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Lykke.HftApi.ApiContract;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.LkkPool.NoSql;

namespace Service.LkkPool.Services
{
    public class PoolManager
    {
        private readonly ILogger<PoolManager> _logger;
        private readonly IMyNoSqlServerDataWriter<PoolConfigNoSql> _configWriter;
        private readonly IMyNoSqlServerDataWriter<LevelModelNoSql> _levelWriter;
        private PoolConfigNoSql _config;
        private TradingApiClient _client;
        private MyTaskTimer _timer;
        private DateTime _lastTradeTime = DateTime.MinValue;
        private List<LevelModelNoSql> _levels;

        public PoolManager(ILogger<PoolManager> logger, 
            IMyNoSqlServerDataWriter<PoolConfigNoSql> configWriter,
            IMyNoSqlServerDataWriter<LevelModelNoSql> levelWriter)
        {
            _logger = logger;
            _configWriter = configWriter;
            _levelWriter = levelWriter;
            _timer = new MyTaskTimer(nameof(PoolManager), TimeSpan.FromMilliseconds(500), logger, HandleTrades);
        }
        
        private async Task HandleTrades()
        {
            if (_config?.Status != ConfigStatus.Active)
                return;

            var request = new TradesRequest()
            {
                AssetPairId = _config.Instrument
            };
            if (_lastTradeTime != DateTime.MinValue)
            {
                request.From = Timestamp.FromDateTime(_lastTradeTime);
            }
            else
            {
                request.Take = 200;
            }
            
            var orders = _client.PrivateApi.GetActiveOrders(new OrdersRequest()
            {
                AssetPairId = _config.Instrument
            });
            
            if (orders.Error != null)
            {
                Console.WriteLine($"ERROR! cannot get active orders: {orders.Error.Code}, {orders.Error.Message}");
                return;
            }

            if (orders.Payload.Count != _levels.Count)
            {
                Console.WriteLine($"Detect disbalance active: {orders.Payload.Count}; memory: {_levels.Count}");
            }
            
            if (orders.Payload.Count >= _levels.Count)
            {
                return;
            }

            var orderDict = orders.Payload.OrderBy(e => decimal.Parse(e.Price))
                .ToDictionary(e => decimal.Parse(e.Price));

            foreach (var level in _levels)
            {
                if (level.IsSellMode)
                {
                    if (!orderDict.TryGetValue(level.SellPrice, out var order))
                    //var order = 
                    //    orders.Payload.FirstOrDefault(e => e.Side == Side.Sell && decimal.Parse(e.Price) == level.SellPrice);
                    //if (order == null)
                    {
                        if (orderDict.Keys.Contains(level.BuyPrice))
                        {
                            Console.WriteLine($"ERROR: try place buy order, but his already exist {level.SellPrice}");
                            level.IsSellMode = false;
                            await _levelWriter.InsertOrReplaceAsync(level);
                            continue;
                        }
                        level.IsSellMode = false;
                        level.CountSell++;
                        
                        var newOrder = _client.PrivateApi.PlaceLimitOrder(new LimitOrderRequest()
                        {
                            AssetPairId = _config.Instrument,
                            Side = Side.Buy,
                            Price = level.BuyPrice.ToString(),
                            Volume = level.Volume.ToString()
                        });

                        if (newOrder.Error != null)
                        {
                            Console.WriteLine($"ERROR! Cannot place BUY order {level.BuyPrice}; {level.Volume}. Error: {newOrder.Error.Code}, {newOrder.Error.Message}");
                            continue;
                        }

                        await _levelWriter.InsertOrReplaceAsync(level);
                        
                        Console.WriteLine($"Place new order Buy {level.BuyPrice}; {level.Volume}. Sell: {level.CountSell}; Buy: {level.CountBuy}");
                    }
                    
                }
                else 
                {
                    //var order = orders.Payload.FirstOrDefault(e => e.Side == Side.Buy && decimal.Parse(e.Price) == level.BuyPrice);
                    //if (order == null)
                    if (!orderDict.TryGetValue(level.BuyPrice, out var order))
                    {
                        if (orderDict.Keys.Contains(level.SellPrice))
                        {
                            Console.WriteLine($"ERROR: try place sell order, but his already exist. {level.SellPrice}");
                            level.IsSellMode = true;
                            await _levelWriter.InsertOrReplaceAsync(level);
                            continue;
                        }
                        
                        level.IsSellMode = true;
                        level.CountBuy++;
                        
                        var newOrder = _client.PrivateApi.PlaceLimitOrder(new LimitOrderRequest()
                        {
                            AssetPairId = _config.Instrument,
                            Side = Side.Sell,
                            Price = level.SellPrice.ToString(),
                            Volume = level.Volume.ToString()
                        });

                        if (newOrder.Error != null)
                        {
                            Console.WriteLine($"ERROR! Cannot place SELL order {level.SellPrice}; {level.Volume}. Error: {newOrder.Error.Code}, {newOrder.Error.Message}");
                            continue;
                        }

                        await _levelWriter.InsertOrReplaceAsync(level);
                        
                        Console.WriteLine($"Place new order Sell {level.SellPrice}; {level.Volume}. Sell: {level.CountSell}; Buy: {level.CountBuy}");
                    }
                }
            }

            await Task.Delay(2000);
        }

        public async Task Start()
        {
            var config = GetConfig();

            if (config.UniqueId != Program.Settings.UniqueId)
            {
                config.UniqueId = Program.Settings.UniqueId;
                config.Instrument = Program.Settings.Instrument;
                config.BaseAsset = Program.Settings.BaseAsset;
                config.QuoteAsset = Program.Settings.QuoteAsset;
                config.CountLevels = Program.Settings.CountLevels;
                config.MinPrice = decimal.Parse(Program.Settings.MinPrice);
                config.MaxPrice = decimal.Parse(Program.Settings.MaxPrice);
                config.MinVolume = decimal.Parse(Program.Settings.MinVolume);
                config.VolumeAccuracy = Program.Settings.VolumeAccuracy;
                config.PriceAccuracy = Program.Settings.PriceAccuracy;
                config.FeePercentage = decimal.Parse(Program.Settings.FeePercentage);
                config.Capital = decimal.Parse(Program.Settings.Capital);
                config.StartPrice = decimal.Parse(Program.Settings.StartPrice);
                config.Status = ConfigStatus.New;
                SaveConfig(config);
            }

            _levels = (await _levelWriter.GetAsync()).ToList();

            _client = new TradingApiClient("https://hft-apiv2-grpc.lykke.com", Program.Settings.ApiKey);

            var isAlive = await _client.MonitoringApi.IsAliveAsync(new IsAliveRequest());
            Console.WriteLine($"Api check:\nName: {isAlive.Name}\nVersion: {isAlive.Version}");

            if (config.Status == ConfigStatus.New)
            {
                await GenerateNewMarket(config);
            }

            _timer.Start();
        }

        private async Task GenerateNewMarket(PoolConfigNoSql config)
        {
            var ppl = (config.MaxPrice - config.MinPrice) / config.CountLevels;
            var cpd = config.FeePercentage / 100 * 2;
            var cpl = config.Capital / config.CountLevels;


            _levels = new List<LevelModelNoSql>();
            
            for (int i = 0; i < config.CountLevels; i++)
            {
                var sellPrice = Math.Round(config.MaxPrice - i * ppl, config.PriceAccuracy);
                
                var level = LevelModelNoSql.Create(Program.Settings.Project, sellPrice);

                level.Id = (i + 1).ToString();
                
                level.SellPrice = sellPrice;
                level.BuyPrice = level.SellPrice - cpd;
                level.Volume = Math.Round(cpl / level.SellPrice, config.VolumeAccuracy);
                if (level.Volume < config.MinVolume)
                {
                    Console.WriteLine($"Level {level.Id} has small volume: {level.Volume}");
                    continue;
                }

                level.IsSellMode = level.SellPrice > config.StartPrice;
                
                _levels.Add(level);
            }

            var baseAmount = _levels
                .Where(e => e.IsSellMode)
                .Sum(e => e.Volume);

            var quoteAmount = _levels
                .Where(e => !e.IsSellMode)
                .Sum(e => e.Volume * e.BuyPrice);

            await _levelWriter.CleanAndKeepMaxRecords(LevelModelNoSql.GeneratePartitionKey(Program.Settings.Project), 0);
            var indb = await _levelWriter.GetAsync(LevelModelNoSql.GeneratePartitionKey(Program.Settings.Project));
            foreach (var item in indb)
            {
                _levelWriter.DeleteAsync(item.PartitionKey, item.RowKey);
            }
            
            indb = await _levelWriter.GetAsync(LevelModelNoSql.GeneratePartitionKey(Program.Settings.Project));
            Console.WriteLine(indb.Count());
            
            await _levelWriter.BulkInsertOrReplaceAsync(_levels);
            indb = await _levelWriter.GetAsync(LevelModelNoSql.GeneratePartitionKey(Program.Settings.Project));
            if (_levels.Count != indb.Count())
            {
                Console.WriteLine($"ERROR: in nosql we has {indb.Count()} levels, but in memory {_levels.Count}");
            }

            await _client.PrivateApi.CancelAllOrdersAsync(new CancelOrdersRequest()
            {
                AssetPairId = config.Instrument,
                Side = Side.Buy
            });
            
            await _client.PrivateApi.CancelAllOrdersAsync(new CancelOrdersRequest()
            {
                AssetPairId = config.Instrument,
                Side = Side.Sell
            });

            var balances = await _client.PrivateApi.GetBalancesAsync(new Empty());

            var baseBalance = 0m;
            var quoteBalance = 0m;
            
            foreach (var balance in balances.Payload)
            {
                Console.WriteLine($"Balance {balance.AssetId}: {balance.Available} ({balance.Reserved})");
                if (balance.AssetId == config.BaseAsset)
                {
                    baseBalance = decimal.Parse(balance.Available);
                }
                if (balance.AssetId == config.QuoteAsset)
                {
                    quoteBalance = decimal.Parse(balance.Available);
                }
            }

            baseAmount += 0.01m;

            if (baseBalance > baseAmount && quoteBalance < quoteAmount)
            {
                var delta = baseBalance - baseAmount;

                delta = Math.Round(delta, config.VolumeAccuracy);
                if (delta > config.MinVolume)
                {
                    var trade = await _client.PrivateApi.PlaceMarketOrderAsync(new MarketOrderRequest()
                    {
                        AssetPairId = config.Instrument,
                        Side = Side.Sell,
                        Volume = delta.ToString()
                    });
                    Console.WriteLine($"Sell trade is {trade.Error?.Code}. Price: {trade.Payload.Price}, Volume: {delta}");
                }
            }
            else if (baseBalance < baseAmount && quoteBalance > quoteAmount)
            {
                var delta = baseAmount- baseBalance;

                delta = Math.Round(delta, config.VolumeAccuracy);
                if (delta > config.MinVolume)
                {
                    var trade = await _client.PrivateApi.PlaceMarketOrderAsync(new MarketOrderRequest()
                    {
                        AssetPairId = config.Instrument,
                        Side = Side.Buy,
                        Volume = delta.ToString()
                    });
                    Console.WriteLine($"Buy trade is {trade.Error?.Code}. Price: {trade.Payload.Price}, Volume: {delta}");
                }
            }

            var bulkRequest = new BulkLimitOrderRequest()
            {
                AssetPairId = config.Instrument,
                CancelMode = CancelMode.BothSides,
                CancelPreviousOrders = true
            };

            foreach (var level in _levels)
            {
                var order = new BulkOrder()
                {
                    Volume = level.Volume.ToString(),
                    Price = level.IsSellMode ? level.SellPrice.ToString() : level.BuyPrice.ToString(),
                    Side = level.IsSellMode ? Side.Sell : Side.Buy
                };
                bulkRequest.Orders.Add(order);
            }
            
            

            await Task.Delay(3000);
            _config.StartTime = DateTime.UtcNow;
            _config.Status = ConfigStatus.Active;
            SaveConfig(_config);
            await Task.Delay(2000);
            
            Console.WriteLine($"Start from {_config.StartTime} and price {_config.StartPrice}");
            balances = await _client.PrivateApi.GetBalancesAsync(new Empty());

            var equity = 0m;
            foreach (var balance in balances.Payload)
            {
                var ba = decimal.Parse(balance.Available);
                
                if (balance.AssetId == config.BaseAsset)
                {
                    ba = ba * _config.StartPrice;
                }
                
                Console.WriteLine($"Balance {balance.AssetId}: {balance.Available} (${ba})");
                equity = equity + ba;
            }
            Console.WriteLine($"Equity: {equity}");

            var result = await _client.PrivateApi.PlaceBulkLimitOrderAsync(bulkRequest);

            Console.WriteLine($"Bulk request count: {bulkRequest.Orders.Count}");
            Console.WriteLine($"Bulk result: {result.Error?.Code ?? ErrorCode.Success}, message: {result.Error?.Message}. Count success: {result.Payload.Statuses.Count(e => e.Error == ErrorCode.Success)}");
            Console.WriteLine("-----------");
            foreach (var status in result.Payload.Statuses.Where(e => e.Error != ErrorCode.Success).OrderByDescending(e => decimal.Parse(e.Price)))
            {
                Console.WriteLine($"Order {status.Error} ID: {status.Id}; Price: {status.Price}; Volume: {status.Volume}");
            }
        }

        private PoolConfigNoSql GetConfig()
        {
            if (_config == null)
            {
                _config = _configWriter
                    .GetAsync(
                        PoolConfigNoSql.GeneratePartitionKey(Program.Settings.Project),
                        PoolConfigNoSql.GenerateRowKey(Program.Settings.Market))
                    .Result;
            }

            if (_config == null)
            {
                _config = PoolConfigNoSql.Create(Program.Settings.Project, Program.Settings.Market);
                SaveConfig(_config);
            }

            return _config;
        }

        private void SaveConfig(PoolConfigNoSql config)
        {
            _config = config;
            _configWriter.InsertOrReplaceAsync(_config).GetAwaiter().GetResult();
        }
    }
}