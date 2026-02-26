using System.Security.Cryptography;
using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _email;

    // In-memory token store for simplicity.
    // For production: store tokens in the DB with expiry, or use a distributed cache.
    private static readonly Dictionary<string, (Guid UserId, DateTime Expiry)> _tokens = new();
    private static readonly object _lock = new();

    public PasswordResetService(ApplicationDbContext context, IEmailService email)
    {
        _context = context;
        _email = email;
    }

    public async Task<string> GenerateResetTokenAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new NotFoundException("No account found with that email address");

        if (user.SsoProvider != null)
            throw new ConflictException("This account uses SSO login. Password reset is not available.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiry = DateTime.UtcNow.AddHours(1);

        lock (_lock)
        {
            // Remove any stale token for this user
            var stale = _tokens.Where(kv => kv.Value.UserId == user.Id).Select(kv => kv.Key).ToList();
            foreach (var k in stale) _tokens.Remove(k);
            _tokens[token] = (user.Id, expiry);
        }

        await _email.SendPasswordResetAsync(email, token);
        return token; // Return token so tests can verify without SMTP in dev
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        (Guid UserId, DateTime Expiry) entry;
        lock (_lock)
        {
            if (!_tokens.TryGetValue(token, out entry))
                throw new UnauthorizedException("Invalid or expired reset token");
        }

        if (entry.Expiry < DateTime.UtcNow)
        {
            lock (_lock) _tokens.Remove(token);
            throw new UnauthorizedException("Reset token has expired. Please request a new one.");
        }

        var user = await _context.Users.FindAsync(entry.UserId)
            ?? throw new NotFoundException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        lock (_lock) _tokens.Remove(token);
    }
}
