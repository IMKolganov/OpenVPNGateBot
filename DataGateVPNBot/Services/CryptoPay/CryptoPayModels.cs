using Newtonsoft.Json;

namespace DataGateVPNBot.Services.CryptoPay;

public sealed class CryptoPayApiResponse<T>
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("result")]
    public T? Result { get; set; }

    [JsonProperty("error")]
    public CryptoPayApiError? Error { get; set; }
}

public sealed class CryptoPayApiError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }
}

public sealed class CryptoPayCreateInvoiceRequest
{
    [JsonProperty("currency_type")]
    public string CurrencyType { get; set; } = "fiat";

    [JsonProperty("fiat")]
    public string Fiat { get; set; } = "USD";

    [JsonProperty("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonProperty("accepted_assets")]
    public string? AcceptedAssets { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("hidden_message")]
    public string? HiddenMessage { get; set; }

    [JsonProperty("paid_btn_name")]
    public string? PaidBtnName { get; set; }

    [JsonProperty("paid_btn_url")]
    public string? PaidBtnUrl { get; set; }

    [JsonProperty("payload")]
    public string? Payload { get; set; }

    [JsonProperty("allow_comments")]
    public bool AllowComments { get; set; } = true;

    [JsonProperty("allow_anonymous")]
    public bool AllowAnonymous { get; set; }

    [JsonProperty("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonProperty("swap_to")]
    public string? SwapTo { get; set; }
}

public sealed class CryptoPayInvoice
{
    [JsonProperty("invoice_id")]
    public long InvoiceId { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("hash")]
    public string? Hash { get; set; }

    [JsonProperty("asset")]
    public string? Asset { get; set; }

    [JsonProperty("fiat")]
    public string? Fiat { get; set; }

    [JsonProperty("amount")]
    public string? Amount { get; set; }

    [JsonProperty("paid_asset")]
    public string? PaidAsset { get; set; }

    [JsonProperty("paid_amount")]
    public string? PaidAmount { get; set; }

    [JsonProperty("fee_asset")]
    public string? FeeAsset { get; set; }

    [JsonProperty("fee_amount")]
    public string? FeeAmount { get; set; }

    [JsonProperty("bot_invoice_url")]
    public string? BotInvoiceUrl { get; set; }

    [JsonProperty("mini_app_invoice_url")]
    public string? MiniAppInvoiceUrl { get; set; }

    [JsonProperty("web_app_invoice_url")]
    public string? WebAppInvoiceUrl { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("payload")]
    public string? Payload { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("paid_anonymously")]
    public bool PaidAnonymously { get; set; }

    public string PayUrl =>
        !string.IsNullOrWhiteSpace(BotInvoiceUrl)
            ? BotInvoiceUrl
            : MiniAppInvoiceUrl ?? WebAppInvoiceUrl ?? string.Empty;
}

public sealed class CryptoPayWebhookUpdate
{
    [JsonProperty("update_id")]
    public long UpdateId { get; set; }

    [JsonProperty("update_type")]
    public string? UpdateType { get; set; }

    [JsonProperty("payload")]
    public CryptoPayInvoice? Payload { get; set; }
}

public sealed class CryptoPayPaidDonation
{
    public long InvoiceId { get; init; }
    public long? TelegramId { get; init; }
    public string Amount { get; init; } = string.Empty;
    public string Asset { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public bool IsDuplicate { get; init; }
}
