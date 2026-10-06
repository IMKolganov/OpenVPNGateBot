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
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            }
            else
            {
                options.KnownIPNetworks.Clear();
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Loopback, 8));
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
        app.MapGet("/.well-known/healthcheck", () => Results.Ok()).ExcludeFromDescription();

        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found", 
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;

        app.MapGet("/",
            (ApplicationRuntimeInfo runtimeInfo,
                IApplicationStartupHistory startupHistory,
                HttpContext context) =>
            {
                var accept = context.Request.Headers.Accept.ToString();
                if (accept.Contains("text/plain", StringComparison.OrdinalIgnoreCase)
                    && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    var plain =
                        $"DataGateVPNBot Application version: {version}; Environment: {environmentName};\nStarted: {runtimeInfo.StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC\nUptime: {RootPageHtml.FormatUptime(runtimeInfo.Uptime)}";
                    return Results.Text(plain, "text/plain; charset=utf-8", statusCode: 200);
                }

                var html = RootPageHtml.Render(
                    version,
                    environmentName,
                    runtimeInfo,
                    startupHistory.GetRecords());
                return Results.Content(html, "text/html; charset=utf-8", statusCode: 200);
            })
            .ExcludeFromDescription();

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}