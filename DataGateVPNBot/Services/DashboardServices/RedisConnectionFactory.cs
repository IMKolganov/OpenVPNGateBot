using DataGateVPNBot.Models.Configurations.Helpers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DataGateVPNBot.Services.DashboardServices;

public class RedisConnectionFactory
{
    private readonly RedisConfig _config;
    private readonly ILogger _logger;

    public RedisConnectionFactory(IOptions<RedisConfig> options, ILogger<RedisConnectionFactory> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    public IConnectionMultiplexer? CreateConnection()
    {
        try
        {
            var options = ConfigurationOptions.Parse(_config.ConnectionString);
            options.ConnectTimeout = 500;
            options.SyncTimeout = 500;
            options.AbortOnConnectFail = false;

            var redis = ConnectionMultiplexer.Connect(options);

            var pong = redis.GetDatabase().Ping();
            _logger.LogInformation($"✅ Connected to Redis. Ping = {pong.TotalMilliseconds} ms");

            return redis;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,$"⚠️ Redis connection failed. ConnectionString: {_config.ConnectionString}");
            return null;
        }
    }
}
