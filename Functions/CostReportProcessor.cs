using Azure.Core;
using Azure.Monitor.Ingestion;
using CDS.CloudOps.CosmosDBWatcher.Models;
using CsvHelper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CDS.CloudOps.CosmosDBWatcher.Functions
{
    public class CostReportProcessor
    {
        private readonly ILogger log;
        private readonly LogsIngestionClient logsIngestionClient;
        private readonly IConfiguration config;

        public CostReportProcessor(ILogger<CostReportProcessor> logger, LogsIngestionClient logsIngestionClient, IConfiguration configuration)
        {
            log = logger;
            config = configuration;
            this.logsIngestionClient = logsIngestionClient;
        }

        [FunctionName("CostReportProcessor")]
        public void Run([BlobTrigger("costexport/{name}/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            var timeGenerated = DateTime.UtcNow;

            log.LogInformation($"Creating a streamreader from the stream");
            using var streamReader = new StreamReader(myBlob);
            log.LogInformation($"Creating a csv reader from the streamreader");
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            log.LogInformation($"Creating a collection of records based on the costdata");
            var records = csvReader.GetRecords<CostData>();

            log.LogInformation($"Pulling only the records for documentdb");
            var cosmosDBRecords = records.Where(record => record.ConsumedService.ToLower() == "microsoft.documentdb").ToList();

            var cosmosDBRecordCount = cosmosDBRecords.Count;
            var uploadCostData = new CostDataUpload[cosmosDBRecordCount];

            log.LogInformation($"Iterating through all the cosmos db records");
            for (int i = 0; i < cosmosDBRecordCount; i++)
            {
                var record = cosmosDBRecords[i];

                log.LogInformation($"Checking if there is additional information");
                if (!string.IsNullOrWhiteSpace(record.AdditionalInfo))
                {
                    log.LogInformation($"Deserializing a cost data additional info from the string");
                    record.AdditionalInfoObject = JsonSerializer.Deserialize<CostDataAdditionalInfo>(record.AdditionalInfo);
                }

                log.LogInformation($"Parsing a resource id from the resource string");
                var resourceId = ResourceIdentifier.Parse(record.ResourceId);

                log.LogInformation($"Creating upload cost data based on the subscription cost data");
                uploadCostData[i] = new CostDataUpload()
                {
                    TimeGenerated = timeGenerated,
                    DatabaseAccountName = record.AdditionalInfoObject?.GlobalDatabaseAccountName ?? resourceId.Name,
                    DatabaseName = record.AdditionalInfoObject?.DatabaseName,
                    ContainerName = record.AdditionalInfoObject?.CollectionName,
                    ContainerRid = record.AdditionalInfoObject?.CollectionRid,
                    UsageTimestamp = record.Date,
                    MeterCategory = record.MeterCategory,
                    MeterSubcategory = record.MeterSubCategory,
                    MeterId = record.MeterId,
                    MeterName = record.MeterName,
                    UsageType = record.AdditionalInfoObject?.UsageType,
                    MeterRegion = record.AdditionalInfoObject?.Region,
                    UsageQuantity = float.Parse(record.Quantity),
                    ResourceRate = float.Parse(record.UnitPrice),
                    PreTaxCost = float.Parse(record.CostInBillingCurrency)
                };
            }

            if (uploadCostData.Length != 0)
            {
                log.LogInformation($"Uploading the data to the log");
                var response = logsIngestionClient.Upload<CostDataUpload>(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdCostData"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameCostData"),
                    uploadCostData);
            }
        }
    }
}
