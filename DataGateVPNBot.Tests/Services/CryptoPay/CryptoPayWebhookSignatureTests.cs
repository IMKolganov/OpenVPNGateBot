using DataGateVPNBot.Services.CryptoPay;
using Xunit;

namespace DataGateVPNBot.Tests.Services.CryptoPay;

public class CryptoPayWebhookSignatureTests
{
    [Fact]
    public void ComputeHex_Is_Stable_For_Known_Token_And_Body()
    {
        var hex = CryptoPayWebhookSignature.ComputeHex("test-token", "{\"ok\":true}");
        Assert.Equal(64, hex.Length);
        Assert.Equal(hex, CryptoPayWebhookSignature.ComputeHex("test-token", "{\"ok\":true}"));
        Assert.NotEqual(hex, CryptoPayWebhookSignature.ComputeHex("other-token", "{\"ok\":true}"));
    }

    [Fact]
    public void IsValid_Accepts_Matching_Signature()
    {
        const string token = "app-token";
        const string body = "{\"update_type\":\"invoice_paid\"}";
        var signature = CryptoPayWebhookSignature.ComputeHex(token, body);

        Assert.True(CryptoPayWebhookSignature.IsValid(token, body, signature));
        Assert.True(CryptoPayWebhookSignature.IsValid(token, body, signature.ToUpperInvariant()));
    }

    [Fact]
    public void IsValid_Rejects_Tampered_Body_Or_Empty()
    {
        const string token = "app-token";
        var signature = CryptoPayWebhookSignature.ComputeHex(token, "body");

        Assert.False(CryptoPayWebhookSignature.IsValid(token, "other", signature));
        Assert.False(CryptoPayWebhookSignature.IsValid(token, "body", "not-hex"));
        Assert.False(CryptoPayWebhookSignature.IsValid("", "body", signature));
        Assert.False(CryptoPayWebhookSignature.IsValid(token, "body", null));
    }
}
