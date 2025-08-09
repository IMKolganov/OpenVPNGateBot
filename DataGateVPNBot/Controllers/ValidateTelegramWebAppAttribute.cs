using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Controllers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateTelegramWebAppAttribute(string payloadArgName = "tgWebAppData", int maxAgeSeconds = 300)
    : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var config = ctx.HttpContext.RequestServices
            .GetRequiredService<IOptions<BotConfiguration>>().Value;

        if (!ctx.ActionArguments.TryGetValue(payloadArgName, out var payload) || payload is null)
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        // Parse incoming payload to JsonDocument
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        var root = doc.RootElement;

        if (!root.TryGetProperty("hash", out var hashEl) || hashEl.ValueKind != JsonValueKind.String)
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        var givenHash = hashEl.GetString()!;
        long authSec = 0;

        if (root.TryGetProperty("auth_date", out var ad))
        {
            if (ad.ValueKind == JsonValueKind.Number)
                authSec = ad.GetInt64();
            else if (ad.ValueKind == JsonValueKind.String &&
                     DateTimeOffset.TryParse(ad.GetString(), out var parsed))
                authSec = parsed.ToUnixTimeSeconds();
        }

        // Build data_check_string from all keys except "hash"
        var dict = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("hash")) continue;
            var val = prop.Value.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Serialize(prop.Value)
                : prop.Value.ToString();
            dict[prop.Name] = val!;
        }

        var dataCheckString = string.Join("\n", dict.Select(kv => $"{kv.Key}={kv.Value}"));

        // secret_key = SHA256(BOT_TOKEN)
        using var sha256 = SHA256.Create();
        var secret = sha256.ComputeHash(Encoding.UTF8.GetBytes(config.BotToken));

        using var hmac = new HMACSHA256(secret);
        var calcHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString)))
            .ToLowerInvariant();

        // Signature mismatch
        if (!calcHex.Equals(givenHash, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        // Expired
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - authSec > maxAgeSeconds)
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        // Save telegram_user_id for controller
        if (root.TryGetProperty("user", out var userEl) &&
            userEl.TryGetProperty("id", out var idEl))
        {
            ctx.HttpContext.Items["telegram_user_id"] = idEl.GetInt64();
        }

        await next();
    }
}
