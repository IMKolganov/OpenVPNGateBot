using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Primitives;

namespace DataGateVPNBot.Middlewares;

public sealed class TmaAuthMiddleware(RequestDelegate next, string botToken, TimeSpan? maxAge = null)
{
    private readonly string _botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
    private readonly TimeSpan _maxAge = maxAge ?? TimeSpan.FromHours(1);
    public const string ContextKey = "tma-init-data";

    public async Task Invoke(HttpContext ctx)
    {
        string? initDataRaw = null;

        var authHeader = ctx.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            var parts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals("tma", StringComparison.OrdinalIgnoreCase))
                initDataRaw = parts[1];
        }

        if (string.IsNullOrWhiteSpace(initDataRaw) &&
            ctx.Request.Headers.TryGetValue("X-TG-WEBAPP-DATA", out StringValues xhdr))
        {
            var v = xhdr.ToString();
            initDataRaw = v.StartsWith("tma ", StringComparison.OrdinalIgnoreCase) ? v[4..] : v;
        }

        if (string.IsNullOrWhiteSpace(initDataRaw))
        {
            await Unauthorized(ctx, "Unauthorized");
            return;
        }

        if (!Validate(initDataRaw, _botToken, _maxAge, out var error))
        {
            await Unauthorized(ctx, error ?? "Unauthorized");
            return;
        }

        var parsed = Parse(initDataRaw, out var parseErr);
        if (parsed is null)
        {
            await Problem(ctx, 500, parseErr ?? "Parse error");
            return;
        }

        ctx.Items[ContextKey] = parsed;
        await next(ctx);
    }

    private static async Task Unauthorized(HttpContext ctx, string message)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonConvert.SerializeObject(new { message }));
    }

    private static async Task Problem(HttpContext ctx, int code, string message)
    {
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonConvert.SerializeObject(new { message }));
    }

    private static bool Validate(string initDataQuery, string botToken, TimeSpan maxAge, out string? error)
    {
        error = null;

        var nvc = System.Web.HttpUtility.ParseQueryString(initDataQuery);
        var givenHash = nvc["hash"];
        if (string.IsNullOrEmpty(givenHash))
        {
            error = "Missing hash";
            return false;
        }

        var pairs = new List<string>();
        foreach (string? key in nvc.AllKeys!)
        {
            if (key is null) continue;
            if (key == "hash" || key == "signature") continue;
            pairs.Add($"{key}={nvc[key]}");
        }
        pairs.Sort(StringComparer.Ordinal);
        var dataCheckString = string.Join("\n", pairs);

        static byte[] Hmac(byte[] key, string data)
        {
            using var h = new HMACSHA256(key);
            return h.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        var secretA = Hmac(Encoding.UTF8.GetBytes("WebAppData"), botToken);
        var calcA = Convert.ToHexString(Hmac(secretA, dataCheckString)).ToLowerInvariant();

        var secretB = Hmac(Encoding.UTF8.GetBytes(botToken), "WebAppData");
        var calcB = Convert.ToHexString(Hmac(secretB, dataCheckString)).ToLowerInvariant();

        if (!calcA.Equals(givenHash, StringComparison.OrdinalIgnoreCase) &&
            !calcB.Equals(givenHash, StringComparison.OrdinalIgnoreCase))
        {
            error = "Invalid hash";
            return false;
        }

        if (!long.TryParse(nvc["auth_date"], out var authSec))
        {
            error = "Invalid auth_date";
            return false;
        }
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - authSec > (long)maxAge.TotalSeconds)
        {
            error = "Auth date expired";
            return false;
        }

        return true;
    }

    public static TmaInitData? Parse(string initDataQuery, out string? error)
    {
        error = null;
        try
        {
            var nvc = System.Web.HttpUtility.ParseQueryString(initDataQuery);
            var data = new TmaInitData
            {
                Raw = initDataQuery,
                QueryId = nvc["query_id"],
                AuthDate = long.TryParse(nvc["auth_date"], out var ts) ? ts : 0,
                StartParam = nvc["start_param"],
                ChatType = nvc["chat_type"],
                ChatInstance = nvc["chat_instance"],
                CanSendAfter = long.TryParse(nvc["can_send_after"], out var csa) ? csa : null,
                Receiver = nvc["receiver"],
                Hash = nvc["hash"]
            };

            var userJson = nvc["user"];
            if (!string.IsNullOrEmpty(userJson))
            {
                data.User = JObject.Parse(userJson);
                if (data.User["id"]?.Type == JTokenType.Integer)
                    data.UserId = data.User["id"]!.Value<long>();
            }

            return data;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }
}