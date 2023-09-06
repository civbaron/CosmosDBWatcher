using System;

namespace CosmosDBWatcher.Models
{
    internal class MetricResultsDataUpload
    {
        public DateTime TimeGenerated { get; set; }
        public string DatabaseAccountName { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public DateTime MetricTimestamp { get; set; }
        public string MetricName { get; set; }
        public double MetricValue { get; set; }
        public object MetricMetadata { get; set; }
    }
}
