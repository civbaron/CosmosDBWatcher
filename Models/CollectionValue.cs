using Azure.Core;
using Azure.ResourceManager.CosmosDB;
using System.Text.Json.Serialization;

namespace CosmosDBWatcher.Models
{
    public class CollectionValue
    {
        public DatabaseValue Database { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public bool IsShared { get; set; }

        [JsonIgnore]
        public ResourceIdentifier CosmosDBSqlContainerIdentifier => CosmosDBSqlContainerResource.CreateResourceIdentifier(
                    Database.DatabaseAccount.Subscription.SubscriptionId,
                    Database.DatabaseAccount.ResourceGroupName,
                    Database.DatabaseAccount.Name,
                    Database.Name,
                    Name);
    }
}
