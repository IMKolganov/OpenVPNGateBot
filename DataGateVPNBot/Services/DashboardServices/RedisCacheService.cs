using StackExchange.Redis;
using System.Text.Json;
using DataGateVPNBot.Models.Redis;

namespace DataGateVPNBot.Services.DashboardServices;

public class RedisCacheService
{
    private readonly IDatabase? _cache;
    private readonly bool _isAvailable;

    public RedisCacheService(IConnectionMultiplexer? redis)
    {
        if (redis?.IsConnected == true)
        {
            _cache = redis.GetDatabase();
            _isAvailable = true;
        }
    }

    public async Task<string?> GetTokenWithExpirationAsync(string key)
    {
        if (!_isAvailable || _cache == null) return null;

        try
        {
            var tokenData = await _cache.StringGetAsync(key);
            if (tokenData.IsNullOrEmpty) return null;

            var tokenObject = JsonSerializer.Deserialize<TokenCacheModel>(tokenData.ToString());
            if (tokenObject != null && tokenObject.Expiration > DateTime.UtcNow)
            {
                return tokenObject.Token;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<RedisCacheService>().Warning(ex, "Failed to get token from Redis with key {Key}", key);
        }

        return null;
    }

    public async Task SetTokenWithExpirationAsync(string key, string token, TimeSpan expiration)
    {
        if (!_isAvailable || _cache == null) return;

        try
        {
            var tokenObject = new TokenCacheModel
            {
                Token = token,
                Expiration = DateTime.UtcNow.Add(expiration)
            };

            var json = JsonSerializer.Serialize(tokenObject);
            await _cache.StringSetAsync(key, json, expiration);
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<RedisCacheService>().Warning(ex, "Failed to set token to Redis with key {Key}", key);
        }
    }
}