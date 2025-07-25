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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        // app.UseStaticFiles(new StaticFileOptions
        // {
        //     ServeUnknownFileTypes = true,
        //     OnPrepareResponse = ctx =>
        //     {
        //         var path = ctx.File.PhysicalPath;
        //         if (path != null && !path.Contains(@"\.well-known\acme-challenge\"))
        //         {
        //             ctx.Context.Response.StatusCode = 404;
        //             ctx.Context.Response.Body = Stream.Null;
        //         }
        //     }
        // });
        
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found", 
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;
        
        app.MapGet("/",
            () => Results.Text(statusCode: 200, 
                content: $"DataGateVPNBot Application version: {version}; Environment: {environmentName};"));
        
        app.MapGet("/.well-known/acme-challenge/{token}", (string token) =>
        {
            if (AcmeChallengeStore.TryGet(token, out var keyAuth))
                return Results.Text(keyAuth, "text/plain");

            return Results.NotFound();
        });

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");
    }
}