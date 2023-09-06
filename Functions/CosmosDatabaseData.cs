using CDS.CloudOps.CosmosDBWatcher.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace CDS.CloudOps.CosmosDBWatcher.Functions
{
    public class CosmosDatabaseData
    {
        [FunctionName("CosmosDatabaseData")]
        [return: Queue("tasks")]
        public CosmosDBWatcherQueueItem Run([TimerTrigger("0 0 1 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            return new CosmosDBWatcherQueueItem()
            {
                ExecutionFunction = nameof(CosmosDBWatcherQueue.GetSubscriptions)
            };
        }
    }
}
