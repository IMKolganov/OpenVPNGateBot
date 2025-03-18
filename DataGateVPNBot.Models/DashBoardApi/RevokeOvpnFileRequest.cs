namespace DataGateVPNBot.Models.DashBoardApi;

public class RevokeOvpnFileRequest
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string CommonName { get; set; } = null!;
}