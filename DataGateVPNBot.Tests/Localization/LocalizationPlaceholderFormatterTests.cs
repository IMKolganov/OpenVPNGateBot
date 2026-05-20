using DataGateVPNBot.Localization;
using Xunit;

namespace DataGateVPNBot.Tests.Localization;

public class LocalizationPlaceholderFormatterTests
{
    [Fact]
    public void Apply_replaces_all_placeholders()
    {
        const string template = "Code: <code>{code}</code>, {minutes} min.";

        var result = LocalizationPlaceholderFormatter.Apply(
            template,
            new Dictionary<string, string>
            {
                ["code"] = "ABCD1234",
                ["minutes"] = "5",
            });

        Assert.Equal("Code: <code>ABCD1234</code>, 5 min.", result);
    }

    [Fact]
    public void Apply_leaves_unknown_placeholders_unchanged()
    {
        const string template = "Hello {name}";

        var result = LocalizationPlaceholderFormatter.Apply(
            template,
            new Dictionary<string, string> { ["code"] = "X" });

        Assert.Equal("Hello {name}", result);
    }
}
