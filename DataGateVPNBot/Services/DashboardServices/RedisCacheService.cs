using StackExchange.Redis;
using System.Text.Json;
using DataGateVPNBot.Models.Redis;

namespace DataGateVPNBot.Services.DashboardServices;

public class RedisCacheService
{
    private readonly IDatabase _cache;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _cache = redis.GetDatabase();
    }

    public async Task<string?> GetTokenWithExpirationAsync(string key)
    {
        var tokenData = await _cache.StringGetAsync(key);

        if (tokenData.IsNullOrEmpty) return null;
        
        var tokenObject = JsonSerializer.Deserialize<TokenCacheModel>(tokenData.ToString());
        
        if (tokenObject != null && tokenObject.Expiration > DateTime.UtcNow)
        {
            return tokenObject.Token;
        }

        return null;
    }

    public async Task SetTokenWithExpirationAsync(string key, string token, TimeSpan expiration)
    {
        var tokenObject = new TokenCacheModel
        {
            Token = token,
            Expiration = DateTime.UtcNow.Add(expiration)
        };

        var json = JsonSerializer.Serialize(tokenObject);
        await _cache.StringSetAsync(key, json, expiration);
    }
}