using Newtonsoft.Json.Linq;

namespace DataGateVPNBot.Middlewares;

public sealed class TmaInitData
{
    public string? Raw { get; set; }
    public string? QueryId { get; set; }
    public long AuthDate { get; set; }
    public string? StartParam { get; set; }
    public string? ChatType { get; set; }
    public string? ChatInstance { get; set; }
    public long? CanSendAfter { get; set; }
    public string? Receiver { get; set; }
    public string? Hash { get; set; }
    public JToken? User { get; set; }
    public long? UserId { get; set; }
}