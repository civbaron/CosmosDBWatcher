using Azure.Core;
using Azure.ResourceManager.CosmosDB;
using System.Text.Json.Serialization;

namespace CosmosDBWatcher.Models
{
    public class DatabaseAccountValue
    {
        public SubscriptionValue Subscription { get; set; }
        public string ResourceGroupName { get; set; }
        public string Name { get; set; }
        public string APIKind { get; set; }
        public string CapacityMode { get; set; }

        [JsonIgnore]
        public ResourceIdentifier CosmosDBAccountIdentifier => CosmosDBAccountResource.CreateResourceIdentifier(
                    Subscription.SubscriptionId,
                    ResourceGroupName,
                    Name);
    }
}
