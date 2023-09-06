using Azure.Core;
using Azure.ResourceManager.CosmosDB;
using System.Text.Json.Serialization;

namespace CDS.CloudOps.CosmosDBWatcher.Models
{
    public class DatabaseValue
    {
        public DatabaseAccountValue DatabaseAccount { get; set; }
        public string DatabaseAccountName { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public ResourceIdentifier CosmosDBSqlDatabaseIdentifier => CosmosDBSqlDatabaseResource.CreateResourceIdentifier(
            DatabaseAccount.Subscription.SubscriptionId,
            DatabaseAccount.ResourceGroupName,
            DatabaseAccount.Name, Name);
    }
}
