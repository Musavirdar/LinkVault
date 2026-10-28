using System.Text;
using Microsoft.Extensions.Configuration;

namespace LinkVaultApi.Services;

public static class StartupValidator
{
    private static readonly string[] InsecurePlaceholders =
    [
        "your-super-secret-key-change-this-in-production",
        "secret",
        "changeme",
        "placeholder",
        "12345678901234567890123456789012"
    ];

    public static void Validate(IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:Secret"] ?? configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("FATAL: JWT secret key is missing. Set JwtSettings:Secret or Jwt:Key in environment variables.");
        }

        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("FATAL: JWT secret key must be at least 256 bits (32 bytes) long for HMAC-SHA256.");
        }

        foreach (var placeholder in InsecurePlaceholders)
        {
            if (secret.Trim().Equals(placeholder, StringComparison.OrdinalIgnoreCase) ||
                secret.Contains("change-this-in-production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"FATAL: Insecure default JWT secret placeholder detected: '{placeholder}'. Replace with a cryptographically secure key.");
            }
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("FATAL: Database connection string 'DefaultConnection' is missing or empty.");
        }
    }
}
