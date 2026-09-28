namespace DataGateVPNBot.Services.Interfaces;

public interface IDashboardAuthAlertService
{
    Task TryNotifyAuthFailureAsync(string reason, CancellationToken cancellationToken = default);
}
