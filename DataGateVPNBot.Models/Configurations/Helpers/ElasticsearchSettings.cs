namespace DataGateVPNBot.Models.Configurations.Helpers;

public class ElasticsearchSettings
{
    public string Uri { get; set; } = "http://localhost:9200";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IndexFormat { get; set; } = "app-logs-{0:yyyy.MM.dd}";
}