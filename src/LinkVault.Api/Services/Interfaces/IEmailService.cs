namespace LinkVault.Api.Services.Interfaces;

public interface IEmailService
{
    Task SendInvitationAsync(string toEmail, string organizationName, string inviterName, string token);
    Task SendWelcomeAsync(string toEmail, string username);
    Task SendPasswordResetAsync(string toEmail, string resetToken);
    Task SendExportApprovedAsync(string toEmail, string fileName);
    Task SendExportDeniedAsync(string toEmail, string fileName, string reason);
}
