using System;

namespace CosmosDBWatcher.Models
{
    internal class CostDataUpload
    {
        public DateTime TimeGenerated { get; set; }
        public string DatabaseAccountName { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public string ContainerRid { get; set; }
        public string UsageTimestamp { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubcategory { get; set; }
        public string MeterId { get; set; }
        public string MeterName { get; set; }
        public string UsageType { get; set; }
        public string MeterRegion { get; set; }
        public float UsageQuantity { get; set; }
        public float ResourceRate { get; set; }
        public float PreTaxCost { get; set; }
    }
}
