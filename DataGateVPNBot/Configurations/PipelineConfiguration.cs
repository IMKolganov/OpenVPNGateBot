using System.Reflection;
using DataGateVPNBot.Services.LetsEncrypt;

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

        app.UseCors(policy =>
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
        );

        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found",
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;

        app.MapGet("/status",
            () => Results.Text(statusCode: 200,
                content: $"DataGateVPNBot Application version: {version}; Environment: {environmentName};"));

        app.MapGet("/.well-known/healthcheck", () => Results.Ok("healthcheck for .well-known"));

        app.MapGet("/.well-known/acme-challenge/{token}", (string token) =>
        {
            if (AcmeChallengeStore.TryGet(token, out var keyAuth))
                return Results.Text(keyAuth, "text/plain");

            return Results.NotFound();
        });

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}