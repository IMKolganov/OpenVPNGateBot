namespace DataGateVPNBot.Models.DashBoardApi;

public class AddOvpnFileRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public int VpnServerId { get; set; }
    public string IssuedTo { get; set; } = "TelegramBot";
}