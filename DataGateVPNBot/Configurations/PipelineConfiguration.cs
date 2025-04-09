using System.Reflection;
using DataGateVPNBot.DataBase.Contexts;
using Microsoft.EntityFrameworkCore;

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
        
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

                if (pendingMigrations.Any())
                {
                    app.Logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                        pendingMigrations.Count, string.Join(", ", pendingMigrations));
                    dbContext.Database.Migrate();
                    app.Logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    app.Logger.LogInformation("Database is up-to-date. No pending migrations.");
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, $"An error occurred while applying migrations: {ex.Message}");
                throw; // optionally rethrow if you want the app to crash
            }
        }
        
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found", 
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;
        
        app.MapGet("/",
            () => Results.Text(statusCode: 200, 
                content: $"DataGateVPNBot Application version: {version}; Environment: {environmentName};"));

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}