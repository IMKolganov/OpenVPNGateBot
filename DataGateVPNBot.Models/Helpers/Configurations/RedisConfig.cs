namespace DataGateVPNBot.Models.Helpers.Configurations;

public class RedisConfig
{
    public string ConnectionString { get; init; } = "redis:6379,abortConnect=false";
}
