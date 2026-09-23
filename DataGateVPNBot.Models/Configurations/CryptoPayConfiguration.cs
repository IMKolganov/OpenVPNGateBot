namespace DataGateVPNBot.Models.Configurations;

public class CryptoPayConfiguration
{
    public const string SectionName = "CryptoPay";
    public const string MainnetApiBaseUrl = "https://pay.crypt.bot/";
    public const string TestnetApiBaseUrl = "https://testnet-pay.crypt.bot/";

    /// <summary>Kill switch. Donations also require <see cref="ApiToken"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>App token from @CryptoBot → /pay → My Apps. Empty = donations disabled.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool UseTestnet { get; set; }

    public string Fiat { get; set; } = "USD";

    /// <summary>Comma-separated USD amounts shown on /donate.</summary>
    public string Amounts { get; set; } = "1,3,5,10,25";

    /// <summary>Crypto assets the payer may choose, comma-separated.</summary>
    public string AcceptedAssets { get; set; } = "USDT,TON,BTC";

    /// <summary>Optional auto-convert of received coins. Empty = keep original asset.</summary>
    public string SwapTo { get; set; } = "USDT";

    public int InvoiceExpiresInSeconds { get; set; } = 3600;

    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(ApiToken);

    public string ApiBaseUrl =>
        UseTestnet ? TestnetApiBaseUrl : MainnetApiBaseUrl;
}
