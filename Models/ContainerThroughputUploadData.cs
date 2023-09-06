using Azure.ResourceManager.CosmosDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBWatcher.Models
{
    internal class ContainerThroughputUploadData
    {
        public DateTime TimeGenerated { get; set; }
        public string DatabaseAccountName { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public string ContainerThroughputMode { get; set; }
        public string ContainerThroughputType { get; set; }
        public int? ContainerThroughput { get; set; }
        public bool ContainerIndexingIsDefault { get; set; }
        public int? ContainerTTL { get; set; }
        public IDictionary<string, object> AdditionalData { get; set; }

        public ContainerThroughputUploadData()
        {

        }

        public ContainerThroughputUploadData(string databaseAccountName, string databaseName, string containerName, string containerThroughputMode, int? containerTTL, bool containerIndexingIsDefault, ExtendedThroughputSettingsResourceInfo throughputSetting, IDictionary<string, object> additionalData)
        {
            TimeGenerated = DateTime.UtcNow;
            DatabaseAccountName = databaseAccountName;
            DatabaseName = databaseName;
            ContainerName = containerName;
            ContainerThroughputMode = containerThroughputMode;
            ContainerIndexingIsDefault = containerIndexingIsDefault;
            ContainerTTL = containerTTL;
            AdditionalData = additionalData;

            if (throughputSetting != null)
            {
                ContainerThroughputType = "Manual";
                ContainerThroughput = throughputSetting.Throughput;

                if (throughputSetting.AutoscaleSettings != null)
                {
                    ContainerThroughputType = "Autoscale";
                    ContainerThroughput = throughputSetting.AutoscaleSettings.MaxThroughput;
                }
            }
            else
            {
                ContainerThroughputType = containerThroughputMode == "Serverless" ? "Serverless" : null;
                ContainerThroughput = null;
            }
        }
    }
}
