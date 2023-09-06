using Azure.ResourceManager.CosmosDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBWatcher.Models
{
    internal class DatabaseThroughputUploadData
    {
        public DateTime TimeGenerated { get; set; }
        public string DatabaseAccountName { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseThroughputMode { get; set; }
        public string DatabaseThroughputType { get; set; }
        public int? DatabaseThroughput { get; set; }
        public IDictionary<string, object> AdditionalData { get; set; }

        public DatabaseThroughputUploadData()
        {

        }

        public DatabaseThroughputUploadData(string databaseAccountName, string databaseName, string databaseThroughputMode, ExtendedThroughputSettingsResourceInfo throughputSetting, IDictionary<string, object> additionalData)
        {
            TimeGenerated = DateTime.UtcNow;
            DatabaseAccountName = databaseAccountName;
            DatabaseName = databaseName;
            DatabaseThroughputMode = databaseThroughputMode;
            AdditionalData = additionalData;

            if (throughputSetting != null)
            {
                DatabaseThroughputType = "Manual";
                DatabaseThroughput = throughputSetting.Throughput;

                if (throughputSetting.AutoscaleSettings != null)
                {
                    DatabaseThroughputType = "Autoscale";
                    DatabaseThroughput = throughputSetting.AutoscaleSettings.MaxThroughput;
                }
            }
            else
            {
                DatabaseThroughputType = databaseThroughputMode == "Serverless" ? "Serverless" : null;
                DatabaseThroughput = null;
            }
        }
    }
}
