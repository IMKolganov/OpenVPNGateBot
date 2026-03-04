using DataGateVPNBot.Helpers;
using DataGateVPNBot.Models.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DataGateVPNBot.Tma;

// Marker to skip auth on specific actions if needed
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class SkipTmaAuthAttribute : Attribute { }

// Put this on a controller or action
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class TmaAuthorizeAttribute() : TypeFilterAttribute(typeof(TmaAuthorizeFilter));

public sealed class TmaAuthorizeFilter : IAsyncActionFilter
{
    private readonly string _botToken;
    private readonly TimeSpan _expIn;
    private readonly ILogger<TmaAuthorizeFilter> _logger;

    public TmaAuthorizeFilter(IOptions<BotConfiguration> options, ILogger<TmaAuthorizeFilter> logger)
    {
        _logger = logger;
        var cfg = options.Value ?? throw new InvalidOperationException("BotConfiguration is not provided.");

        _botToken = string.IsNullOrWhiteSpace(cfg.BotToken)
            ? throw new InvalidOperationException("BotConfiguration.BotToken is not configured.")
            : cfg.BotToken;

        _expIn = cfg.InitDataLifetime is { } ts && ts > TimeSpan.Zero
            ? ts
            : TimeSpan.FromHours(1);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Allow bypass on a specific action
        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipTmaAuthAttribute>().Any())
        {
            await next();
            return;
        }

        var http = context.HttpContext;
        var raw = TryGetInitData(http, out var source);

        if (string.IsNullOrWhiteSpace(raw))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Init data not found" });
            return;
        }
        var validator = new TelegramInitDataValidator(_botToken);
        var parsed = validator.ValidateAndParse(raw, expiresInSeconds: 3600);

        http.Items["TmaInitDataRaw"] = parsed;

        await next();
    }

    private static string? TryGetInitData(HttpContext http, out string source)
    {
        if (http.Request.Headers.TryGetValue("Telegram-Init-Data", out var h1))
        { source = "Telegram-Init-Data header"; return h1.ToString(); }

        if (http.Request.Headers.TryGetValue("X-Init-Data", out var h2))
        { source = "X-Init-Data header"; return h2.ToString(); }

        if (http.Request.Query.TryGetValue("initData", out var q))
        { source = "query string"; return q.ToString(); }

        if (http.Request.HasFormContentType && http.Request.Form.TryGetValue("initData", out var f))
        { source = "form field"; return f.ToString(); }

        source = "none";
        return null;
    }
}
