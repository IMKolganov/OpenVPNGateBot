using DataGateVPNBot.Helpers;
using Xunit;

namespace DataGateVPNBot.Tests;

public class TelegramInitDataTests
{
    [Fact]
    public void FromDictionary_EmptyDict_ReturnsDefaultValues()
    {
        var kv = new Dictionary<string, string>();
        var result = TelegramInitData.FromDictionary(kv);
        Assert.Null(result.User);
        Assert.Equal(0, result.AuthDateUnix);
        Assert.Null(result.ChatType);
        Assert.Null(result.ChatInstance);
    }

    [Fact]
    public void FromDictionary_WithAuthDate_ParsesUnix()
    {
        var kv = new Dictionary<string, string> { ["auth_date"] = "1730000000" };
        var result = TelegramInitData.FromDictionary(kv);
        Assert.Equal(1730000000, result.AuthDateUnix);
    }

    [Fact]
    public void FromDictionary_WithChatTypeAndInstance_SetsValues()
    {
        var kv = new Dictionary<string, string>
        {
            ["chat_type"] = "private",
            ["chat_instance"] = "123"
        };
        var result = TelegramInitData.FromDictionary(kv);
        Assert.Equal("private", result.ChatType);
        Assert.Equal("123", result.ChatInstance);
    }

    [Fact]
    public void FromDictionary_WithValidUserJson_ParsesUser()
    {
        var userJson = """{"id":12345,"username":"test","first_name":"Test","last_name":null}""";
        var kv = new Dictionary<string, string> { ["user"] = userJson };
        var result = TelegramInitData.FromDictionary(kv);
        Assert.NotNull(result.User);
        Assert.Equal(12345, result.User!.Id);
        Assert.Equal("test", result.User.Username);
        Assert.Equal("Test", result.User.FirstName);
        Assert.Null(result.User.LastName);
    }

    [Fact]
    public void FromDictionary_InvalidUserJson_UserRemainsNull()
    {
        var kv = new Dictionary<string, string> { ["user"] = "not-valid-json" };
        var result = TelegramInitData.FromDictionary(kv);
        Assert.Null(result.User);
    }
}
