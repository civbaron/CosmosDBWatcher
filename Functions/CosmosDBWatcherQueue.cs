using Azure.Identity;
using Azure.Monitor.Ingestion;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using CosmosDBWatcher.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CosmosDBWatcher.Functions
{
    public class CosmosDBWatcherQueue
    {
        private readonly ILogger<CosmosDBWatcherQueue> log;
        private readonly LogsIngestionClient logsIngestionClient;
        private readonly IConfiguration config;

        public CosmosDBWatcherQueue(ILogger<CosmosDBWatcherQueue> logger,
            LogsIngestionClient logsIngestionClient,
            IConfiguration configuration)
        {
            log = logger;
            this.logsIngestionClient = logsIngestionClient;
            this.config = configuration;
        }

        [FunctionName(nameof(CosmosDBWatcherQueue))]
        public void Run([QueueTrigger("tasks")] string myQueueItem, [Queue("tasks")] ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            var queueData = JsonSerializer.Deserialize<CosmosDBWatcherQueueItem>(myQueueItem);

            log.LogInformation($"Function: {queueData.ExecutionFunction}");

            switch (queueData.ExecutionFunction)
            {
                case nameof(GetSubscriptions):
                    GetSubscriptions(
                        new List<string>()
                        {
                            nameof(GetDatabaseAccounts)
                        }, collector);
                    break;
                case nameof(GetDatabaseAccounts):
                    GetDatabaseAccounts(queueData, collector);
                    break;
                case nameof(GetDatabases):
                    GetDatabases(queueData, collector);
                    break;
                case nameof(GetDatabaseThroughput):
                    GetDatabaseThroughput(queueData, collector);
                    break;
                case nameof(GetCollections):
                    GetCollections(queueData, collector);
                    break;
                case nameof(GetCollectionThroughput):
                    GetCollectionThroughput(queueData, collector);
                    break;
                case nameof(GetCollectionRequestMetrics):
                    GetCollectionRequestMetrics(queueData);
                    break;
                case nameof(GetCollectionPKUsageMetrics):
                    GetCollectionPKUsageMetrics(queueData);
                    break;
                case nameof(GetCollectionPartitionSizeMetrics):
                    GetCollectionPartitionSizeMetrics(queueData);
                    break;
                case nameof(GetCollectionStorageMetrics):
                    GetCollectionStorageMetrics(queueData);
                    break;
            }
        }

        public void GetSubscriptions(List<string> nextFunctions, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            log.LogInformation("Calling to get all subscriptions for default tenant");
            var subscriptions = armClient.GetSubscriptions().GetAll();

            log.LogInformation("Iterating through all subscriptions");
            foreach (var subscription in subscriptions)
            {
                log.LogInformation("Adding subscription resource to collection to return");
                foreach (var nextFunction in nextFunctions)
                {
                    collector.Add(new CosmosDBWatcherQueueItem()
                    {
                        ExecutionFunction = nextFunction,
                        Payload = JsonSerializer.Serialize(new SubscriptionValue()
                        {
                            SubscriptionId = subscription.Id.SubscriptionId,
                            Name = subscription.Data.DisplayName
                        })
                    });
                }
            }
        }

        private void GetDatabaseAccounts(CosmosDBWatcherQueueItem queueItem, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            var subscriptionValue = JsonSerializer.Deserialize<SubscriptionValue>(queueItem.Payload);

            log.LogInformation("Calling to get subscription resource");
            var subscription = armClient.GetSubscriptionResource(subscriptionValue.SubscriptionIdentifier).Get().Value;

            log.LogInformation("Calling to get all database accounts for subscription");
            var databaseAccounts = subscription.GetCosmosDBAccounts();

            var timeGenerated = DateTime.UtcNow;
            log.LogInformation("Iterating through all database accounts");
            foreach (var databaseAccount in databaseAccounts)
            {
                var apiKind = GetApiKind(databaseAccount);
                var capacityMode = GetCapacityMode(databaseAccount);
                log.LogInformation($"Creating the database account data object");
                var databaseAccountData = new List<object>() {
                    new {
                        TimeGenerated = timeGenerated,
                        databaseAccount.Data.Id.SubscriptionId,
                        SubscriptionName = subscriptionValue.Name,
                        ResourceGroup = databaseAccount.Data.Id.ResourceGroupName,
                        DatabaseAccountName = databaseAccount.Data.Name,
                        APIKind = apiKind,
                        CapacityMode = capacityMode,
                        AdditionalData = databaseAccount.ToDictionary()
                    }};

                log.LogInformation($"Uploading the database acount data object");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdDatabaseAccountsConfig"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameDatabaseAccountsConfig"),
                    databaseAccountData);

                log.LogInformation("Adding database account resource to collection to return");
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetDatabases),
                    Payload = JsonSerializer.Serialize(new DatabaseAccountValue()
                    {
                        Subscription = subscriptionValue,
                        ResourceGroupName = databaseAccount.Id.ResourceGroupName,
                        Name = databaseAccount.Data.Name,
                        APIKind = apiKind,
                        CapacityMode = capacityMode
                    })
                });
            }
        }

        private void GetDatabases(CosmosDBWatcherQueueItem queueItem, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            var databaseAccountValue = JsonSerializer.Deserialize<DatabaseAccountValue>(queueItem.Payload);

            log.LogInformation("Calling to get database account resource");
            var databaseAccount = armClient.GetCosmosDBAccountResource(databaseAccountValue.CosmosDBAccountIdentifier).Get().Value;

            log.LogInformation("Calling to get all databases for database account");
            var databases = databaseAccount.GetCosmosDBSqlDatabases();

            log.LogInformation("Iterating through all databases");
            foreach (var database in databases)
            {
                log.LogInformation("Adding database resource to collection to return");
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetDatabaseThroughput),
                    Payload = JsonSerializer.Serialize(new DatabaseValue()
                    {
                        DatabaseAccount = databaseAccountValue,
                        Name = database.Data.Name
                    })
                });
            }
        }

        public void GetDatabaseThroughput(CosmosDBWatcherQueueItem queueItem, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            var databaseValue = JsonSerializer.Deserialize<DatabaseValue>(queueItem.Payload);

            log.LogInformation("Calling to get database account resource");
            var database = armClient.GetCosmosDBSqlDatabaseResource(databaseValue.CosmosDBSqlDatabaseIdentifier).Get().Value;

            DatabaseThroughputUploadData databaseThroughputInfo;
            try
            {
                log.LogInformation($"Getting the database throughput settings");
                var noSQLThroughput = database.GetCosmosDBSqlDatabaseThroughputSetting();

                if (!noSQLThroughput.HasData)
                {
                    noSQLThroughput = noSQLThroughput.Get().Value;
                }

                log.LogInformation($"Creating a database throughput upload data record");
                databaseThroughputInfo = new DatabaseThroughputUploadData(
                    databaseValue.DatabaseAccount.Name,
                    databaseValue.Name,
                    "Shared",
                    noSQLThroughput.Data.Resource,
                    database.ToDictionary());
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                if (ex.Message.Contains("Status: 400") &&
                    ex.Message.Contains("Reading or replacing offers is not supported for serverless accounts"))
                {
                    databaseThroughputInfo = new DatabaseThroughputUploadData(
                        databaseValue.DatabaseAccount.Name,
                        databaseValue.Name,
                        "Serverless",
                        null,
                        database.ToDictionary());
                }
                else if (ex.Message.Contains("Status: 404"))
                {
                    databaseThroughputInfo = new DatabaseThroughputUploadData(
                        databaseValue.DatabaseAccount.Name,
                        databaseValue.Name,
                        "Dedicated",
                        null,
                        database.ToDictionary());
                }
                else
                {
                    throw new Exception("Error Occured trying to pull the database throughput settings");
                }
            }

            if (databaseThroughputInfo != null && !string.IsNullOrWhiteSpace(databaseThroughputInfo.DatabaseAccountName))
            {
                log.LogInformation($"Uploading the database throuhgput data");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdDatabasesConfig"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameDatabasesConfig"),
                    new List<DatabaseThroughputUploadData>() { databaseThroughputInfo });

                log.LogInformation("Adding queueitem to get collections");
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollections),
                    Payload = JsonSerializer.Serialize(databaseValue)
                });
            }
        }

        public void GetCollections(CosmosDBWatcherQueueItem queueItem, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            var databaseValue = JsonSerializer.Deserialize<DatabaseValue>(queueItem.Payload);

            log.LogInformation("Calling to get database resource");
            var database = armClient.GetCosmosDBSqlDatabaseResource(databaseValue.CosmosDBSqlDatabaseIdentifier);

            log.LogInformation("Calling to get all containers for database");
            var collections = database.GetCosmosDBSqlContainers();

            log.LogInformation("Iterating through all containers");
            foreach (var collection in collections)
            {
                log.LogInformation("Adding collection resource to collection to return");
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollectionThroughput),
                    Payload = JsonSerializer.Serialize(new CollectionValue()
                    {
                        Database = databaseValue,
                        Identifier = collection.Id,
                        Name = collection.Data.Name
                    })
                });
            }
        }

        public void GetCollectionThroughput(CosmosDBWatcherQueueItem queueItem, ICollector<CosmosDBWatcherQueueItem> collector)
        {
            log.LogInformation("Getting a new armclient");
            var armClient = new ArmClient(new DefaultAzureCredential());

            var collectionValue = JsonSerializer.Deserialize<CollectionValue>(queueItem.Payload);

            log.LogInformation("Calling to get container resource");
            var containerResource = armClient.GetCosmosDBSqlContainerResource(collectionValue.CosmosDBSqlContainerIdentifier).Get().Value;
            var isDefault = GetIsIndexingDefault(containerResource.Data.Resource);
            var containerTTL = containerResource.Data.Resource.DefaultTtl;
            ContainerThroughputUploadData containerThroughputData;

            try
            {
                log.LogInformation($"Pulling the container throughput setting");
                var containerThroughput = containerResource.GetCosmosDBSqlContainerThroughputSetting();

                if (!containerThroughput.HasData)
                {
                    containerThroughput = containerThroughput.Get().Value;
                }

                log.LogInformation($"Creating a container throughput data object");
                containerThroughputData = new ContainerThroughputUploadData(
                    collectionValue.Database.DatabaseAccount.Name,
                    collectionValue.Database.Name,
                    collectionValue.Name,
                    "Dedicated",
                    containerTTL,
                    isDefault,
                    containerThroughput.Data.Resource,
                    containerResource.ToDictionary());
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Status: 400") &&
                    ex.Message.Contains("Reading or replacing offers is not supported for serverless accounts"))
                {
                    containerThroughputData = new ContainerThroughputUploadData(
                        collectionValue.Database.DatabaseAccount.Name,
                        collectionValue.Database.Name,
                        collectionValue.Name,
                        "Serverless",
                        containerTTL,
                        isDefault,
                        null,
                        containerResource.ToDictionary());
                }
                else if (ex.Message.Contains("Status: 404"))
                {
                    containerThroughputData = new ContainerThroughputUploadData(
                        collectionValue.Database.DatabaseAccount.Name,
                        collectionValue.Database.Name,
                        collectionValue.Name,
                        "Shared",
                        containerTTL,
                        isDefault,
                        null,
                        containerResource.ToDictionary());
                }
                else
                {
                    throw new Exception("Error Occured trying to pull the container throughput settings");
                }
            }

            if (!string.IsNullOrWhiteSpace(containerThroughputData.DatabaseAccountName))
            {
                log.LogInformation($"Uploading the container throughput data to the logs");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdContainersConfig"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameContainersConfig"),
                    new List<ContainerThroughputUploadData>() { containerThroughputData });

                collectionValue.IsShared = containerThroughputData.ContainerThroughputMode == "Shared";

                log.LogInformation("Adding collection resource to do metrics");
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollectionRequestMetrics),
                    Payload = JsonSerializer.Serialize(collectionValue)
                });
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollectionPKUsageMetrics),
                    Payload = JsonSerializer.Serialize(collectionValue)
                });
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollectionPartitionSizeMetrics),
                    Payload = JsonSerializer.Serialize(collectionValue)
                });
                collector.Add(new CosmosDBWatcherQueueItem()
                {
                    ExecutionFunction = nameof(GetCollectionStorageMetrics),
                    Payload = JsonSerializer.Serialize(collectionValue)
                });
            }
        }

        public void GetCollectionRequestMetrics(CosmosDBWatcherQueueItem queueItem)
        {
            log.LogInformation($"Creating a metrics query client");
            var metricsClient = new MetricsQueryClient(new DefaultAzureCredential());

            var collectionValue = JsonSerializer.Deserialize<CollectionValue>(queueItem.Payload);

            log.LogInformation($"Creating a metrics query options");
            var metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(1),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '{collectionValue.Name}' and OperationType eq '*' and Region eq '*' and StatusCode eq '*'"
            };

            log.LogInformation($"Adding metric query options aggregations");
            metricQueryOptions.Aggregations.Add(Azure.Monitor.Query.Models.MetricAggregationType.Count);

            log.LogInformation($"Calling to get a metric results for the query");
            var results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "TotalRequests",
                    "TotalRequestUnits"
                },
                metricQueryOptions).Value;

            var metricResults = new List<MetricResultsDataUpload>();
            var timeGenerated = DateTime.UtcNow;

            log.LogInformation($"Iterating through the metric results");
            foreach (var metric in results.Metrics)
            {
                log.LogInformation($"Iterating through the metric result timeseries");
                foreach (var timeSeries in metric.TimeSeries)
                {
                    log.LogInformation($"Iterating through the metric result timeseries values");
                    foreach (var metricValue in timeSeries.Values)
                    {
                        log.LogInformation($"Creating a metadata ocject from timeseries object");
                        var metaData = new
                        {
                            OperationType = timeSeries.Metadata["operationtype"],
                            Region = timeSeries.Metadata["region"],
                            StatusCode = int.Parse(timeSeries.Metadata["statuscode"])
                        };

                        log.LogInformation($"Adding and creating a metricresultsdata object");
                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Count ?? 0,
                            MetricMetadata = metaData
                        });
                    }
                }
            }

            if (metricResults.Any())
            {
                log.LogInformation("Uploading metric results");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdContainersMetrics"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameContainersMetrics"),
                    metricResults);
            }
        }

        public void GetCollectionPKUsageMetrics(CosmosDBWatcherQueueItem queueItem)
        {
            log.LogInformation($"Creating a metricQueryClient");
            var metricsClient = new MetricsQueryClient(new DefaultAzureCredential());

            log.LogInformation($"Deserializing the payload");
            var collectionValue = JsonSerializer.Deserialize<CollectionValue>(queueItem.Payload);

            log.LogInformation($"Figuring out the containername");
            var containerName = collectionValue.IsShared ? "<empty>" : collectionValue.Name;

            log.LogInformation($"Creating metric options");
            var metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(1),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '{containerName}' and Region eq '*' and PartitionKeyRangeId eq '*' and PhysicalPartitionId eq '*'"
            };

            log.LogInformation($"Querying the resource");
            var results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "NormalizedRUConsumption"
                },
                metricQueryOptions).Value;

            var metricResults = new List<MetricResultsDataUpload>();

            var timeGenerated = DateTime.UtcNow;
            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        var metaData = new
                        {
                            Region = timeSeries.Metadata["region"],
                            PartitionKeyRangeId = timeSeries.Metadata["partitionkeyrangeid"],
                            PhysicalPartitionId = timeSeries.Metadata["physicalpartitionid"]
                        };

                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Maximum ?? 0,
                            MetricMetadata = metaData
                        });
                    }
                }
            }

            if (metricResults.Any())
            {
                log.LogInformation("Uploading metric results");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdContainersMetrics"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameContainersMetrics"),
                    metricResults);
            }
        }

        public void GetCollectionPartitionSizeMetrics(CosmosDBWatcherQueueItem queueItem)
        {
            log.LogInformation($"Creating a metricQueryClient");
            var metricsClient = new MetricsQueryClient(new DefaultAzureCredential());

            log.LogInformation($"Deserializing the payload");
            var collectionValue = JsonSerializer.Deserialize<CollectionValue>(queueItem.Payload);

            log.LogInformation($"Figuring out the containername");
            var containerName = collectionValue.IsShared ? "<empty>" : collectionValue.Name;

            log.LogInformation($"Creating metric options");
            var metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(1),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '{containerName}' and Region eq '*' and PhysicalPartitionId eq '*'"
            };

            log.LogInformation($"Querying the resource");
            var results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "PhysicalPartitionSizeInfo"
                },
                metricQueryOptions).Value;

            var metricResults = new List<MetricResultsDataUpload>();

            var timeGenerated = DateTime.UtcNow;
            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        var metaData = new
                        {
                            Region = timeSeries.Metadata["region"],
                            PhysicalPartitionId = timeSeries.Metadata["physicalpartitionid"],
                            PartitionSize = metricValue.Maximum
                        };

                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Maximum ?? 0,
                            MetricMetadata = metaData
                        });
                    }
                }
            }

            if (metricResults.Any())
            {
                log.LogInformation("Uploading metric results");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdContainersMetrics"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameContainersMetrics"),
                    metricResults);
            }
        }

        public void GetCollectionStorageMetrics(CosmosDBWatcherQueueItem queueItem)
        {
            log.LogInformation($"Creating a metrics query client");
            var metricResults = new List<MetricResultsDataUpload>();

            var collectionValue = JsonSerializer.Deserialize<CollectionValue>(queueItem.Payload);

            if (collectionValue.IsShared)
            {
                log.LogInformation($"Calling to get metrics for a shared throughput");
                metricResults.AddRange(GetSharedStorageMetrics(collectionValue));
            }
            else
            {
                log.LogInformation($"Calling to get metrics for a non shared throughput");
                metricResults.AddRange(GetStorageMetrics(collectionValue));
            }

            if (metricResults.Any())
            {
                log.LogInformation("Uploading storage metric results");
                logsIngestionClient.Upload(
                    config.GetValue<string>("AzureMonitorDataCollectionRuleIdContainersMetrics"),
                    config.GetValue<string>("AzureMonitorDataCollectionStreamNameContainersMetrics"),
                    metricResults);
            }
        }

        internal List<MetricResultsDataUpload> GetSharedStorageMetrics(CollectionValue collectionValue)
        {
            var metricsClient = new MetricsQueryClient(new DefaultAzureCredential());
            var metricResults = new List<MetricResultsDataUpload>();

            var metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(5),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '__Empty'"
            };

            var results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "ProvisionedThroughput",
                    "AutoscaleMaxThroughput"
                },
                metricQueryOptions).Value;

            var timeGenerated = DateTime.UtcNow;
            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Maximum ?? 0,
                            MetricMetadata = null
                        });
                    }
                }
            }

            metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(5),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '{collectionValue.Name}'"
            };

            results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "DataUsage",
                    "DocumentCount"
                },
                metricQueryOptions).Value;

            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Total ?? 0,
                            MetricMetadata = null
                        });
                    }
                }
            }

            metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(5),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}'"
            };

            results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "IndexUsage"
                },
                metricQueryOptions).Value;

            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        metricResults.Add(new MetricResultsDataUpload()
                        {
                            TimeGenerated = timeGenerated,
                            DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                            DatabaseName = collectionValue.Database.Name,
                            ContainerName = collectionValue.Name,
                            MetricTimestamp = metricValue.TimeStamp.DateTime,
                            MetricName = metric.Name,
                            MetricValue = metricValue.Total ?? 0,
                            MetricMetadata = null
                        });
                    }
                }
            }

            return metricResults;
        }

        internal List<MetricResultsDataUpload> GetStorageMetrics(CollectionValue collectionValue)
        {
            var metricsClient = new MetricsQueryClient(new DefaultAzureCredential());
            var metricResults = new List<MetricResultsDataUpload>();

            var metricQueryOptions = new MetricsQueryOptions()
            {
                TimeRange = new QueryTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
                Granularity = TimeSpan.FromMinutes(5),
                MetricNamespace = "Microsoft.DocumentDB/DatabaseAccounts",
                Filter = $"DatabaseName eq '{collectionValue.Database.Name}' and CollectionName eq '{collectionValue.Name}'"
            };

            var results = metricsClient.QueryResource(
                collectionValue.Database.DatabaseAccount.CosmosDBAccountIdentifier.ToString(),
                new List<string>()
                {
                    "ProvisionedThroughput",
                    "AutoscaleMaxThroughput",
                    "DataUsage",
                    "IndexUsage",
                    "DocumentCount"
                },
                metricQueryOptions).Value;

            var timeGenerated = DateTime.UtcNow;

            foreach (var metric in results.Metrics)
            {
                foreach (var timeSeries in metric.TimeSeries)
                {
                    foreach (var metricValue in timeSeries.Values)
                    {
                        if (metric.Name == "ProvisionedThroughput" || metric.Name == "AutoscaleMaxThroughput")
                        {
                            metricResults.Add(new MetricResultsDataUpload()
                            {
                                TimeGenerated = timeGenerated,
                                DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                                DatabaseName = collectionValue.Database.Name,
                                ContainerName = collectionValue.Name,
                                MetricTimestamp = metricValue.TimeStamp.DateTime,
                                MetricName = metric.Name,
                                MetricValue = metricValue.Maximum ?? 0,
                                MetricMetadata = null
                            });
                        }
                        else
                        {
                            metricResults.Add(new MetricResultsDataUpload()
                            {
                                TimeGenerated = timeGenerated,
                                DatabaseAccountName = collectionValue.Database.DatabaseAccount.Name,
                                DatabaseName = collectionValue.Database.Name,
                                ContainerName = collectionValue.Name,
                                MetricTimestamp = metricValue.TimeStamp.DateTime,
                                MetricName = metric.Name,
                                MetricValue = metricValue.Total ?? 0,
                                MetricMetadata = null
                            });
                        }
                    }
                }
            }

            return metricResults;
        }

        public bool GetIsIndexingDefault(ExtendedCosmosDBSqlContainerResourceInfo resource)
        {
            var cosmosDBIndexingPolicy = new CosmosDBIndexingPolicy()
            {
                IsAutomatic = true,
                IndexingMode = CosmosDBIndexingMode.Consistent
            };

            cosmosDBIndexingPolicy.IncludedPaths.Add(new CosmosDBIncludedPath()
            {
                Path = "/*"
            });

            cosmosDBIndexingPolicy.ExcludedPaths.Add(new CosmosDBExcludedPath()
            {
                Path = "/\"_etag\"/?"
            });

            return resource.IndexingPolicy == cosmosDBIndexingPolicy;
        }

        private string GetCapacityMode(CosmosDBAccountResource databaseAccount)
        {
            log.LogInformation($"Getting capacity mode based on capabilities");
            return databaseAccount.Data.Capabilities.Any(x => x.Name == "EnableServerless") ? "Serverless" : "Provisioned throughput";
        }

        private string GetApiKind(CosmosDBAccountResource databaseAccount)
        {
            log.LogInformation($"Figuring out the api kind based on the capabilities");
            return databaseAccount.Data.Capabilities.Any(x => x.Name == "EnableMongo") ? "Mongo" :
                databaseAccount.Data.Capabilities.Any(x => x.Name == "EnableCassandra") ? "Cassandra" :
                databaseAccount.Data.Capabilities.Any(x => x.Name == "EnableTable") ? "Table" :
                databaseAccount.Data.Capabilities.Any(x => x.Name == "EnableGremlin") ? "Gremlin" :
                "NoSQL";
        }
    }
}
