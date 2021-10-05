using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.LkkPool.Settings
{
    public class SettingsModel
    {
        [YamlProperty("LkkPool.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("LkkPool.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("LkkPool.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("LkkPool.NoSqlWriterUrl")]
        public string NoSqlWriterUrl { get; set; }
        
        [YamlProperty("LkkPool.Project")]
        public string Project { get; set; }
        
        [YamlProperty("LkkPool.Market")]
        public string Market { get; set; }
        
        [YamlProperty("LkkPool.ApiKey")]
        public string ApiKey { get; set; }
        
        [YamlProperty("LkkPool.Config_UniqueId")]
        public string UniqueId { get; set; }
        
        [YamlProperty("LkkPool.Config_Instrument")]
        public string Instrument { get; set; }
        
        [YamlProperty("LkkPool.Config_BaseAsset")]
        public string BaseAsset { get; set; }
        
        [YamlProperty("LkkPool.Config_QuoteAsset")]
        public string QuoteAsset { get; set; }
        
        [YamlProperty("LkkPool.Config_MinPrice")]
        public string MinPrice { get; set; }
        
        [YamlProperty("LkkPool.Config_MaxPrice")]
        public string MaxPrice { get; set; }
        
        [YamlProperty("LkkPool.Config_StartPrice")]
        public string StartPrice { get; set; }
        
        [YamlProperty("LkkPool.Config_CountLevels")]
        public int CountLevels { get; set; }
        
        [YamlProperty("LkkPool.Config_MinVolume")]
        public string MinVolume { get; set; }
        
        [YamlProperty("LkkPool.Config_VolumeAccuracy")]
        public int VolumeAccuracy { get; set; }
        
        [YamlProperty("LkkPool.Config_PriceAccuracy")]
        public int PriceAccuracy { get; set; }
        
        [YamlProperty("LkkPool.Config_FeePercentage")]
        public string FeePercentage { get; set; }
        
        [YamlProperty("LkkPool.Config_Capital")]
        public string Capital { get; set; }
        
        
    }
}
