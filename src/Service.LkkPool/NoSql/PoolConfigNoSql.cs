using System;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;

namespace Service.LkkPool.NoSql
{
    public class PoolConfigNoSql: MyNoSqlDbEntity
    {
        public const string TableName = "jetwallet-pool1";
        public static string GeneratePartitionKey(string project) => project;
        public static string GenerateRowKey(string market) => market;

        public static PoolConfigNoSql Create(string project, string market)
        {
            return new PoolConfigNoSql()
            {
                PartitionKey = GeneratePartitionKey(project),
                RowKey = GenerateRowKey(market)
            };
        }
        
        public string UniqueId { get; set; }
        
        public string Instrument { get; set; }
        
        public string BaseAsset { get; set; }
        
        public string QuoteAsset { get; set; }
        
        public decimal MinPrice { get; set; }
        
        public decimal MaxPrice { get; set; }
        
        public decimal StartPrice { get; set; }
        
        public int CountLevels { get; set; }
        
        public decimal MinVolume { get; set; }
        
        public int VolumeAccuracy { get; set; }
        
        public int PriceAccuracy { get; set; }
        
        public decimal FeePercentage { get; set; }
        
        public decimal Capital { get; set; }
        
        public ConfigStatus Status { get; set; }
        public DateTime StartTime { get; set; }
    }

    public enum ConfigStatus
    {
        New,
        Active
    }
}