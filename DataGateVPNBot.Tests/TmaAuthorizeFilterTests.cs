using System.Security.Cryptography;
using System.Text;
using DataGateVPNBot.Helpers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Tma;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests;

public class TmaAuthorizeFilterTests
{
    private static string BuildValidInitData(string botToken, long authDateUnix)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal) { ["auth_date"] = authDateUnix.ToString() };
        var dataCheck = string.Join("\n", dict.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
        var secretKey = HmacSha256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hashBytes = HmacSha256(secretKey, Encoding.UTF8.GetBytes(dataCheck));
        var hashHex = ToHexLower(hashBytes);
        dict["hash"] = hashHex;
        return string.Join("&", dict.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string ToHexLower(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    [Fact]
    public async Task OnActionExecutionAsync_When_SkipTmaAuth_Attribute_Present_Calls_Next()
    {
        const string botToken = "test-token";
        var options = Options.Create(new BotConfiguration { BotToken = botToken });
        var filter = new TmaAuthorizeFilter(options, Mock.Of<ILogger<TmaAuthorizeFilter>>());
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = new List<object> { new SkipTmaAuthAttribute() } };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
        var nextCalled = false;
        ActionExecutionDelegate next = () => { nextCalled = true; return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!) { Result = new OkResult() }); };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_When_InitData_Missing_Returns_Unauthorized()
    {
        var options = Options.Create(new BotConfiguration { BotToken = "token" });
        var filter = new TmaAuthorizeFilter(options, Mock.Of<ILogger<TmaAuthorizeFilter>>());
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = new List<object>() };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
        ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));

        await filter.OnActionExecutionAsync(context, next);

        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task OnActionExecutionAsync_When_Valid_InitData_In_Header_Sets_Items_And_Calls_Next()
    {
        const string botToken = "test-bot-token";
        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var initData = BuildValidInitData(botToken, authDate);
        var options = Options.Create(new BotConfiguration { BotToken = botToken });
        var filter = new TmaAuthorizeFilter(options, Mock.Of<ILogger<TmaAuthorizeFilter>>());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Telegram-Init-Data"] = initData;
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = new List<object>() };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
        var nextCalled = false;
        ActionExecutionDelegate next = () => { nextCalled = true; return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!) { Result = new OkResult() }); };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
        Assert.True(httpContext.Items.ContainsKey("TmaInitDataRaw"));
        var parsed = httpContext.Items["TmaInitDataRaw"] as TelegramInitData;
        Assert.NotNull(parsed);
        Assert.Equal(authDate, parsed.AuthDateUnix);
    }
}
