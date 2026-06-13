using DataGateVPNBot.Handlers;
using Xunit;

namespace DataGateVPNBot.Tests;

public class TelegramCopyTextHelperTests
{
    [Fact]
    public void TryGetVlessCopyText_ValidSingleLine_ReturnsVlessUri()
    {
        const string input = "vless://uuid@example.com:443?encryption=none&security=tls&type=tcp#DataGate";

        var result = TelegramCopyTextHelper.TryGetVlessCopyText(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void TryGetVlessCopyText_MultiLineTemplate_ReturnsFirstVlessLine()
    {
        const string vless = "vless://uuid@95.111.204.102:8443?encryption=none&security=tls&sni=xs1.datagateapp.com&type=tcp#DataGate";
        var input = $"{vless}\n# Friendly name\nUUID: uuid\nEndpoint: 95.111.204.102:8443\n";

        var result = TelegramCopyTextHelper.TryGetVlessCopyText(input);

        Assert.Equal(vless, result);
    }

    [Fact]
    public void TryGetVlessCopyText_NoVlessLine_ReturnsNull()
    {
        const string input = "# Friendly name\nUUID: uuid\nEndpoint: 95.111.204.102:8443\n";

        var result = TelegramCopyTextHelper.TryGetVlessCopyText(input);

        Assert.Null(result);
    }

    [Fact]
    public void TryGetVlessCopyText_VlessTooLong_ReturnsNull()
    {
        var longTail = new string('a', 300);
        var input = $"vless://uuid@example.com:443?encryption=none&type=tcp&note={longTail}#DataGate";

        var result = TelegramCopyTextHelper.TryGetVlessCopyText(input);

        Assert.Null(result);
    }
}
