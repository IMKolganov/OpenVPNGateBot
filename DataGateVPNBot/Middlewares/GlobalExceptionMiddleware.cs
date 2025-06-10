using System.Net;
using DataGateVPNBot.Services.Interfaces;
using Newtonsoft.Json;

namespace DataGateVPNBot.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    IServiceProvider serviceProvider,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
            using var scope = serviceProvider.CreateScope();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            errorService.LogErrorToDatabase(ex, context); //todo: fix it
            await errorService.NotifyAdminsAsync(ex, context);
            await HandleExceptionAsync(context); //ex);

        }
    }

    private static Task HandleExceptionAsync(HttpContext context)//, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            context.Response.StatusCode,
            Message = "An unexpected error occurred. Please try again later.",
            // Detail = exception.Message
        };

        return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}