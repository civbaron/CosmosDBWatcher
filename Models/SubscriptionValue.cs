using Azure.Core;
using Azure.ResourceManager.Resources;
using System.Text.Json.Serialization;

namespace CDS.CloudOps.CosmosDBWatcher.Models
{
    public class SubscriptionValue
    {

        public string SubscriptionId { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public ResourceIdentifier SubscriptionIdentifier => SubscriptionResource.CreateResourceIdentifier(
            SubscriptionId);
    }
}
