namespace DataGateVPNBot.Models.Redis;

public class TokenCacheModel
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}