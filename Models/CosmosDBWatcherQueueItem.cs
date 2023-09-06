namespace CDS.CloudOps.CosmosDBWatcher.Models
{
    public class CosmosDBWatcherQueueItem
    {
        public string ExecutionFunction { get; set; }
        public string Payload { get; set; }
    }
}
