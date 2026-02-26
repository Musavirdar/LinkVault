using System.Net;
using System.Net.Mail;
using LinkVault.Api.Services.Interfaces;

namespace LinkVault.Api.Services;

/// <summary>
/// SMTP email service. Configure "Email" section in appsettings.json:
/// {
///   "Email": {
///     "Host": "smtp.sendgrid.net",
///     "Port": 587,
///     "Username": "apikey",
///     "Password": "YOUR_SENDGRID_API_KEY",
///     "From": "noreply@linkvault.app",
///     "FromName": "LinkVault"
///   }
/// }
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendInvitationAsync(string toEmail, string organizationName, string inviterName, string token)
    {
        var acceptUrl = $"{_config["App:BaseUrl"]}/invitations/{token}/accept";
        var subject = $"You've been invited to join {organizationName} on LinkVault";
        var body = $"""
            <h2>You've been invited!</h2>
            <p><strong>{inviterName}</strong> has invited you to join <strong>{organizationName}</strong> on LinkVault.</p>
            <p>Click the link below to create your account. This invitation expires in <strong>7 days</strong>.</p>
            <p><a href="{acceptUrl}" style="background:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;">Accept Invitation</a></p>
            <p style="color:#6B7280;font-size:12px;">Or copy this link: {acceptUrl}</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendWelcomeAsync(string toEmail, string username)
    {
        var subject = "Welcome to LinkVault!";
        var body = $"""
            <h2>Welcome, {username}!</h2>
            <p>Your LinkVault account has been created. Start organising your links and files today.</p>
            <p style="color:#6B7280;font-size:12px;">If you didn't create this account, please ignore this email.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetToken)
    {
        var resetUrl = $"{_config["App:BaseUrl"]}/reset-password?token={resetToken}";
        var subject = "Reset your LinkVault password";
        var body = $"""
            <h2>Password Reset</h2>
            <p>We received a request to reset your password. This link expires in 1 hour.</p>
            <p><a href="{resetUrl}" style="background:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block;">Reset Password</a></p>
            <p style="color:#6B7280;font-size:12px;">If you didn't request this, you can safely ignore this email.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendExportApprovedAsync(string toEmail, string fileName)
    {
        var subject = "Export Request Approved — LinkVault";
        var body = $"""
            <h2>Export Request Approved ✅</h2>
            <p>Your request to download <strong>{fileName}</strong> has been approved by your organization admin.</p>
            <p>Login to LinkVault to download the file.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendExportDeniedAsync(string toEmail, string fileName, string reason)
    {
        var subject = "Export Request Denied — LinkVault";
        var body = $"""
            <h2>Export Request Denied ❌</h2>
            <p>Your request to download <strong>{fileName}</strong> was denied.</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p>Contact your organization administrator if you believe this is incorrect.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var host = _config["Email:Host"];
            var port = int.Parse(_config["Email:Port"] ?? "587");
            var username = _config["Email:Username"];
            var password = _config["Email:Password"];
            var from = _config["Email:From"] ?? "noreply@linkvault.app";
            var fromName = _config["Email:FromName"] ?? "LinkVault";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // Don't throw — email failure should not break the primary request
        }
    }
}
