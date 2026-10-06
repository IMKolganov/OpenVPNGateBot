using System.Globalization;
using System.Net;
using System.Text;

namespace DataGateVPNBot.Configurations;

public static class RootPageHtml
{
    private const string ProjectSiteUrl = "https://datagateapp.com/";
    private const string StatusPageUrl = "https://status.datagateapp.com/";
    private const string GitHubUrl = "https://github.com/IMKolganov/DataGateVPNBot";
    private const string FaviconUrl =
        "https://raw.githubusercontent.com/IMKolganov/DataGateMonitorFrontend/main/public/favicon.svg";

    public static string Render(
        string version,
        string environmentName,
        ApplicationRuntimeInfo runtimeInfo,
        IReadOnlyList<ApplicationStartupRecord> startupHistory)
    {
        var startedAt = runtimeInfo.StartedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var uptime = FormatUptime(runtimeInfo.Uptime);

        var sb = new StringBuilder(4096);
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.Append("<title>DataGate VPN Bot</title>");
        sb.Append("<link rel=\"icon\" type=\"image/svg+xml\" href=\"");
        sb.Append(H(FaviconUrl));
        sb.Append("\"/>");
        sb.Append("""
<style>
:root { color-scheme: dark; }
* { box-sizing: border-box; }
body {
  margin: 0;
  min-height: 100vh;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
  background: #0d1117;
  color: #c9d1d9;
  line-height: 1.5;
}
.wrap { max-width: 720px; margin: 0 auto; padding: 2.5rem 1.25rem 3rem; }
header { margin-bottom: 1.75rem; }
.logo { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.75rem; }
.logo img { width: 36px; height: 36px; }
h1 { margin: 0; font-size: 1.75rem; font-weight: 600; color: #f0f6fc; }
.lead { margin: 0; color: #8b949e; font-size: 1rem; }
.card {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
  margin-bottom: 1.25rem;
}
.card h2 { margin: 0 0 1rem; font-size: 0.85rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; color: #8b949e; }
dl { margin: 0; display: grid; grid-template-columns: minmax(8rem, 11rem) 1fr; gap: 0.65rem 1rem; }
dt { color: #8b949e; font-size: 0.92rem; }
dd { margin: 0; color: #f0f6fc; font-size: 0.95rem; word-break: break-word; }
.history-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.history-table th,
.history-table td { padding: 0.55rem 0.65rem; text-align: left; border-bottom: 1px solid #21262d; vertical-align: top; }
.history-table th { color: #8b949e; font-size: 0.78rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em; }
.history-table td { color: #c9d1d9; word-break: break-word; }
.history-table tr:last-child td { border-bottom: none; }
.history-table tr.current td { color: #f0f6fc; }
.history-empty { margin: 0; color: #8b949e; font-size: 0.9rem; }
.links { display: flex; flex-wrap: wrap; gap: 0.75rem; }
.links a {
  display: inline-flex;
  align-items: center;
  padding: 0.45rem 0.85rem;
  border-radius: 999px;
  border: 1px solid #30363d;
  background: #21262d;
  color: #58a6ff;
  text-decoration: none;
  font-size: 0.92rem;
}
.links a:hover { border-color: #58a6ff; background: #1f2937; }
footer { margin-top: 2rem; text-align: center; font-size: 0.85rem; color: #6e7681; }
@media (max-width: 520px) {
  dl { grid-template-columns: 1fr; gap: 0.25rem; }
  dt { margin-top: 0.5rem; }
}
</style>
</head><body><div class="wrap">
""");

        sb.Append("<header><div class=\"logo\"><img src=\"");
        sb.Append(H(FaviconUrl));
        sb.Append("\" alt=\"\" width=\"36\" height=\"36\"/><h1>DataGate VPN Bot</h1></div>");
        sb.Append("<p class=\"lead\">ASP.NET Core Telegram bot for DataGate: registration, VPN config delivery, localization, and integration with DataGate Monitor API.</p>");
        sb.Append("</header>");

        sb.Append("<section class=\"card\"><h2>Instance</h2><dl>");
        AppendRow(sb, "Version", version);
        AppendRow(sb, "Environment", environmentName);
        AppendRow(sb, "Started", startedAt);
        AppendRow(sb, "Uptime", uptime);
        sb.Append("</dl></section>");

        sb.Append("<section class=\"card\"><h2>Links</h2><div class=\"links\">");
        AppendLink(sb, "DataGate", ProjectSiteUrl);
        AppendLink(sb, "Service status", StatusPageUrl);
        AppendLink(sb, "GitHub", GitHubUrl);
        sb.Append("</div></section>");

        AppendStartupHistory(sb, startupHistory);

        sb.Append("<footer>DataGate VPN Telegram bot</footer></div></body></html>");
        return sb.ToString();
    }

    private static void AppendStartupHistory(StringBuilder sb, IReadOnlyList<ApplicationStartupRecord> startupHistory)
    {
        sb.Append("<section class=\"card\"><h2>Startup history</h2>");

        if (startupHistory.Count == 0)
        {
            sb.Append("<p class=\"history-empty\">No startup records yet.</p></section>");
            return;
        }

        sb.Append("<table class=\"history-table\"><thead><tr>");
        sb.Append("<th>Started</th><th>Version</th><th>Environment</th>");
        sb.Append("</tr></thead><tbody>");

        for (var i = 0; i < startupHistory.Count; i++)
        {
            var record = startupHistory[i];
            sb.Append("<tr");
            if (i == 0)
                sb.Append(" class=\"current\"");
            sb.Append("><td>");
            sb.Append(H(FormatTimestamp(record.StartedAtUtc)));
            sb.Append("</td><td>");
            sb.Append(H(record.Version));
            sb.Append("</td><td>");
            sb.Append(H(record.Environment));
            sb.Append("</td></tr>");
        }

        sb.Append("</tbody></table></section>");
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

    private static void AppendRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<dt>");
        sb.Append(H(label));
        sb.Append("</dt><dd>");
        sb.Append(H(value));
        sb.Append("</dd>");
    }

    private static void AppendLink(StringBuilder sb, string label, string url)
    {
        sb.Append("<a href=\"");
        sb.Append(H(url));
        sb.Append("\" rel=\"noopener noreferrer\" target=\"_blank\">");
        sb.Append(H(label));
        sb.Append("</a>");
    }

    public static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return string.Create(CultureInfo.InvariantCulture, $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m");

        if (uptime.TotalHours >= 1)
            return string.Create(CultureInfo.InvariantCulture, $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");

        if (uptime.TotalMinutes >= 1)
            return string.Create(CultureInfo.InvariantCulture, $"{uptime.Minutes}m {uptime.Seconds}s");

        return string.Create(CultureInfo.InvariantCulture, $"{uptime.Seconds}s");
    }

    private static string H(string value) => WebUtility.HtmlEncode(value);
}
