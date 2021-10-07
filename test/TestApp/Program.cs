using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Lykke.HftApi.ApiContract;
using ProtoBuf.Grpc.Client;
using Service.LkkPool.Client;
using Service.LkkPool.Grpc.Models;
using Service.LkkPool.Services;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var apikey = "";
            
            var client = new TradingApiClient("https://hft-apiv2-grpc.lykke.com", apikey);

            var ts = DateTime.Parse("2021-10-05T19:43:44Z");
            ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
            
            Console.WriteLine(ts);
            
            
            var data = await client.PrivateApi.GetTradesAsync(new TradesRequest()
            {
                AssetPairId = "ETHUSD",
                From = Timestamp.FromDateTime(ts),
                Take = 1000
            });

            var eth = 0m;
            var usd = 0m;
            
            foreach (var trade in data.Payload.OrderBy(e => e.Timestamp))
            {
                eth += (trade.Side == Side.Buy ? 1 : -1) * decimal.Parse(trade.BaseVolume);
                usd += (trade.Side == Side.Buy ? -1 : 1) * decimal.Parse(trade.QuoteVolume);
                Console.WriteLine($"{trade.Timestamp:HH:mm:ss} {trade.Side} {trade.BaseVolume} {trade.Price} [{trade.QuoteVolume}]");
            }
            
            Console.WriteLine();
            Console.WriteLine("---------");
            Console.WriteLine();
            Console.WriteLine($"Eth: {eth};  usd: {usd}");
            Console.WriteLine($"Equity: {eth*3500 + usd} $");

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
