namespace DataGateVPNBot.Services.BotServices;

public sealed class VpnProfileDownload
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
}
