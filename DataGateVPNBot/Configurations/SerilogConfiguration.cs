using DataGateVPNBot.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace DataGateVPNBot.Configurations;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this IHostBuilder host)
    {
        var elasticConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("elasticsearch.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .MinimumLevel.Information();

        var elasticsearchSettings = new ElasticsearchSettings
        {
            Uri = (Environment.GetEnvironmentVariable("ELASTIC_URI") 
                   ?? elasticConfig["Elasticsearch:Uri"]) ?? string.Empty,

            Username = (Environment.GetEnvironmentVariable("ELASTIC_USERNAME") 
                        ?? elasticConfig["Elasticsearch:Username"]) ?? string.Empty,

            Password = (Environment.GetEnvironmentVariable("ELASTIC_PASSWORD") 
                        ?? elasticConfig["Elasticsearch:Password"]) ?? string.Empty,

            IndexFormat = (Environment.GetEnvironmentVariable("ELASTIC_INDEX_FORMAT") 
                           ?? elasticConfig["Elasticsearch:IndexFormat"]) ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(elasticsearchSettings.Uri))
        {
            loggerConfig = loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchSettings.Uri))
            {
                IndexFormat = elasticsearchSettings.IndexFormat ?? "default-index",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
                NumberOfShards = 1,
                NumberOfReplicas = 0,
                ModifyConnectionSettings = conn => conn
                    .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
                    .BasicAuthentication(elasticsearchSettings.Username, elasticsearchSettings.Password),
                FailureCallback = (logEvent, exception) =>
                {
                    Console.WriteLine($"Unable to submit event: {logEvent.RenderMessage()}");
                    if (exception != null)
                        Console.WriteLine($"Exception: {exception.Message}");
                },
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
            });

            Console.WriteLine($"Elasticsearch logging is enabled. " +
                              $"Host: {elasticsearchSettings.Uri} IndexFormat: {elasticsearchSettings.IndexFormat}");
        }
        else
        {
            Console.WriteLine("Elasticsearch settings not found. Logging to console only.");
        }

        Log.Logger = loggerConfig.CreateLogger();
        host.UseSerilog();
    }
}