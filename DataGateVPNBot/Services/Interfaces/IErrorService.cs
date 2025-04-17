namespace DataGateVPNBot.Services.Interfaces;

public interface IErrorService
{
    void LogErrorToDatabase(Exception exception, HttpContext? context = null);
    Task NotifyAdminsAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default);
    Task NotifyAdminsAboutStartAsync(CancellationToken cancellationToken = default);
}