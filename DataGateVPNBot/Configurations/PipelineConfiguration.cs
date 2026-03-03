using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;

namespace DataGateVPNBot.Configurations;

public static class PipelineConfiguration
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardedForHeaderName = "X-Forwarded-For",
                ForwardedProtoHeaderName = "X-Forwarded-Proto"
            };
            if (app.Configuration.GetValue<bool>("ForwardedHeaders:AllowAll"))
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
            else
            {
                options.KnownNetworks.Clear();
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Loopback, 8));
            }
            app.UseForwardedHeaders(options);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found", 
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;
        
        app.MapGet("/",
            (ILogger<Program> logger) => Results.Text(statusCode: 200,
                content: $"DataGateVPNBot Application version: {version}; Environment: {environmentName};"));

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}