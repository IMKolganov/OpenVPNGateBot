namespace DataGateVPNBot.Localization;

public static class LocalizationPlaceholderFormatter
{
    public static string Apply(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(placeholders);

        foreach (var (name, value) in placeholders)
            template = template.Replace($"{{{name}}}", value, StringComparison.Ordinal);

        return template;
    }
}
