using System.Net;
using DataGateVPNBot.Services.Interfaces;
using Newtonsoft.Json;

namespace DataGateVPNBot.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next,
        IServiceProvider serviceProvider,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            using var scope = _serviceProvider.CreateScope();
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