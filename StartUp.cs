using Azure.Identity;
using Azure.Monitor.Ingestion;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(CosmosDBWatcher.StartUp))]
namespace CosmosDBWatcher
{
    public class StartUp : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var credentials = new DefaultAzureCredential();
            var appConfigName = Environment.GetEnvironmentVariable("APPCONFIGURATIONNAME");
            builder.ConfigurationBuilder
                .AddAzureAppConfiguration(options =>
                    options.Connect(new Uri($"https://{appConfigName}.azconfig.io"), credentials)
                        .ConfigureKeyVault(options =>
                            options.SetCredential(credentials))
                        .ConfigureRefresh(options =>
                            options.Register("Sentinel", true)
                                .SetCacheExpiration(new TimeSpan(0, 0, 30))));
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;

            var logIngestionUrl = config.GetValue<string>("AzureMonitorDataCollectionEndpoint");
                builder.Services.AddSingleton(
                    new LogsIngestionClient(
                        new Uri(logIngestionUrl),
                        new DefaultAzureCredential()
                        )
                    );
        }
    }
}
