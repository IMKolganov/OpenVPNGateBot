using System.Reflection;

namespace DataGateVPNBot.Helpers;

/// <summary>
/// Reads <c>IsDisabled</c> from <see cref="DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto.VpnServerDto"/>
/// when the installed SharedModels package includes that property (reflection keeps the bot buildable on older package versions).
/// </summary>
internal static class VpnServerDtoReflection
{
    public static bool IsDisabled(object? server)
    {
        if (server == null) return false;
        var p = server.GetType().GetProperty("IsDisabled", BindingFlags.Public | BindingFlags.Instance);
        return p?.PropertyType == typeof(bool) && p.GetValue(server) is true;
    }
}
