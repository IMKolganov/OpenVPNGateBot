using DataGateVPNBot.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace DataGateVPNBot.Configurations;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this IHostBuilder host, IConfiguration configuration)
    {
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .MinimumLevel.Information();

        var elasticSection = configuration.GetSection("Elasticsearch");
        var elasticsearchSettings = elasticSection.Exists()
            ? elasticSection.Get<ElasticsearchSettings>()
            : null;

        if (elasticsearchSettings != null &&
            !string.IsNullOrWhiteSpace(elasticsearchSettings.Uri))
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

            Console.WriteLine("Elasticsearch logging is enabled.");
        }
        else
        {
            Console.WriteLine("Elasticsearch settings not found. Logging to console only.");
        }

        Log.Logger = loggerConfig.CreateLogger();
        host.UseSerilog();
    }
}