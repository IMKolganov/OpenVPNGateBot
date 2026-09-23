using System.Text;
using DataGateVPNBot.Models.Configurations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateVPNBot.Services.CryptoPay;

public sealed class CryptoPayApiClient(
    HttpClient httpClient,
    IOptions<CryptoPayConfiguration> options,
    ILogger<CryptoPayApiClient> logger) : ICryptoPayApiClient
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task<CryptoPayInvoice> CreateInvoiceAsync(
        CryptoPayCreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = options.Value;
        if (!config.IsConfigured)
            throw new InvalidOperationException("Crypto Pay is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/createInvoice");
        message.Headers.TryAddWithoutValidation("Crypto-Pay-API-Token", config.ApiToken);
        message.Content = new StringContent(
            JsonConvert.SerializeObject(request, JsonSettings),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        CryptoPayApiResponse<CryptoPayInvoice>? parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<CryptoPayApiResponse<CryptoPayInvoice>>(json, JsonSettings);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Crypto Pay returned non-JSON for createInvoice. Status={Status}", response.StatusCode);
            throw new InvalidOperationException("Crypto Pay returned an unexpected response.");
        }

        if (parsed is not { Ok: true, Result: not null })
        {
            var error = parsed?.Error?.Name ?? $"HTTP {(int)response.StatusCode}";
            logger.LogWarning("Crypto Pay createInvoice failed: {Error}. Body={Body}", error, json);
            throw new InvalidOperationException($"Crypto Pay createInvoice failed: {error}");
        }

        return parsed.Result;
    }
}
