using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("openvpn-api")]
public class OpenVpnApiController : ControllerBase
{
    /// <summary>
    /// This endpoint responds to HEAD requests from the OpenVPN Connect app.
    /// It returns a 200 OK and sets custom headers to inform the client about the web auth method.
    /// </summary>
    /// <remarks>
    /// This is a workaround. I don't know any other way to interact reliably with OpenVPN Connect.
    /// The app expects this endpoint to exist and return 200, otherwise it refuses to proceed with profile import.
    /// </remarks>
    [HttpHead("profile")]
    public IActionResult OpenVpnProfileProbe()
    {
        // Extract incoming headers (not used yet, but may be useful for diagnostics or logging)
        var h = Request.Headers;

        // Required by OpenVPN Connect: tells it not to expect any auth interaction
        Response.Headers.Append("Ovpn-WebAuth", "none");

        // Optional alternative:
        // Response.Headers.Append("Ovpn-WebAuth-Optional", "FlowerVPN,name=Smartcard SAML authentication,external");

        return Ok(); // Must return 200 for OpenVPN Connect to proceed
    }


    [HttpGet("profile")]
    public IActionResult OpenVpnProfilePage()
    {
        var token = Request.Query["token"].ToString();

        if (string.IsNullOrEmpty(token))
            return BadRequest("Missing token");

        var redirectUri =
            $"openvpn://import-profile/https://gate.rackot.ru/DownloadByToken?token={token}";

        var scriptBlock = $@"
    <script>
        let secondsLeft = 3;

        function updateCountdown() {{
            const countdown = document.getElementById('redirect-timer');
            countdown.textContent = secondsLeft;
            secondsLeft--;

            if (secondsLeft >= 0) {{
                setTimeout(updateCountdown, 1000);
            }}
        }}

        setTimeout(() => {{
            window.location.href = '{redirectUri}';
        }}, 3000);

        window.addEventListener('DOMContentLoaded', updateCountdown);

        function copyToClipboard() {{
            const input = document.getElementById('vpn-link');
            navigator.clipboard.writeText(input.value)
                .then(() => {{
                    const btn = document.getElementById('copy-button');
                    btn.textContent = 'Copied!';
                    setTimeout(() => btn.textContent = 'Copy', 3000);
                }})
                .catch(err => console.error('Failed to copy:', err));
        }}
    </script>
";

        var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>OpenVPN Gate Monitor – Redirecting</title>
    {scriptBlock}
    <script src=""https://cdn.tailwindcss.com""></script>
</head>
<body class=""bg-[#0d1117] text-[#c9d1d9] font-sans flex flex-col items-center justify-center min-h-screen px-4"">
    <div class=""text-center max-w-xl flex-grow flex flex-col justify-center"">
        <h1 class=""text-3xl font-bold mb-2"">OpenVPN Gate Monitor</h1>
        <p class=""text-sm text-gray-400 mb-6 italic"">
            Part of the <strong class=""font-semibold"">OpenVPN Gate Monitor</strong> project family
        </p>

        <div class=""bg-[#161b22] border border-[#30363d] p-6 rounded-lg shadow-sm"">
            <h2 class=""text-xl font-semibold mb-3"">Telegram Bot Redirect</h2>
            <p class=""mb-4"">This Telegram bot helps you connect to VPN services provided by OpenVPN Gate Monitor.</p>
            <p class=""text-lg mb-4"">
                Redirecting to your OpenVPN profile in <span id=""redirect-timer"">3</span> seconds…
            </p>

            <div class=""flex flex-col sm:flex-row items-stretch gap-2 mb-4"">
                <input
                    id=""vpn-link""
                    type=""text""
                    value=""{redirectUri}""
                    readonly
                    class=""w-full sm:w-auto flex-grow px-3 py-2 rounded bg-[#0d1117] border border-[#30363d] text-sm text-[#c9d1d9] focus:outline-none""
                />
                <button
                    id=""copy-button""
                    onclick=""copyToClipboard()""
                    class=""px-4 py-2 rounded bg-[#238636] hover:bg-[#2ea043] text-white text-sm font-medium transition""
                >
                    Copy
                </button>
            </div>

            <p>If nothing happens, <a href=""{redirectUri}"" class=""text-[#58a6ff] underline"">click here manually</a>.</p>
        </div>
    </div>

    <footer class=""mt-10 text-center text-sm text-gray-500 pb-4"">
        <p>
            Telegram bot: <a href=""https://t.me/OpenVPNGateBot"" class=""underline text-[#58a6ff]"">@OpenVPNGateBot</a>
            &nbsp;|&nbsp;
            GitHub: <a href=""https://github.com/imkolganov/OpenVPNGateMonitor"" class=""underline text-[#58a6ff]"">OpenVPNGateMonitor</a>
        </p>
    </footer>
</body>
</html>
";



        return Content(html, "text/html");
    }
}