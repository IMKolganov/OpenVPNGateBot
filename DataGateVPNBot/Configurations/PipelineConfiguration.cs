using System.Reflection;
using DataGateVPNBot.Services.UntilsServices;

namespace DataGateVPNBot.Configurations;

public static class PipelineConfiguration
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
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
            (ILogger<EasyRsaService> logger) => Results.Text(statusCode: 200, 
                content: $"DataGateVPNBot Application version: {version}; Environment: {environmentName};"));

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}