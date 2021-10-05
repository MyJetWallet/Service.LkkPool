using MyNoSqlServer.Abstractions;

namespace Service.LkkPool.NoSql
{
    public class LevelModelNoSql: MyNoSqlDbEntity
    {
        public const string TableName = "jetwallet-pool1-levels";
        public static string GeneratePartitionKey(string project) => project;
        public static string GenerateRowKey(string market) => market;
        
        public static LevelModelNoSql Create(string market, decimal sellPrice)
        {
            return new LevelModelNoSql()
            {
                PartitionKey = GeneratePartitionKey(market),
                RowKey = GenerateRowKey(sellPrice.ToString())
            };
        }
        
        public string Id { get; set; }
        
        public decimal Volume { get; set; }
        
        public decimal SellPrice { get; set; }
        
        public decimal BuyPrice { get; set; }
        
        public decimal StartPrice { get; set; }

        public bool IsSellMode { get; set; }
        
        public int CountSell { get; set; }
        public int CountBuy { get; set; }
    }
}