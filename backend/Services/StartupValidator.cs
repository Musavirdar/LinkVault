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
        "12345678901234567890123456789012",
        "linkvault-default-key-override-with-jwt-secret-in-production-env",
        "lv-dev-secret-key-a8f3b9c2e1d4a7f6b5c8e2d1f4a9b3c7",
        "your-super-secret-key-that-is-at-least-32-characters-long-for-jwt-signing",
        "replace_this_with_a_very_long_secure_random_string_for_production"
    ];

    private static readonly string[] InsecurePasswords =
    [
        "linkvault123",
        "QHMroGZ1oxMou8G7x8qwgwds24Uk7kIg"
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
                secret.Contains("change-this-in-production", StringComparison.OrdinalIgnoreCase) ||
                secret.Contains("override-with-jwt-secret", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"FATAL: Insecure default JWT secret placeholder detected: '{placeholder}'. Replace with a cryptographically secure key.");
            }
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("FATAL: Database connection string 'DefaultConnection' is missing or empty.");
        }

        foreach (var insecurePassword in InsecurePasswords)
        {
            if (connectionString.Contains(insecurePassword, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"FATAL: Insecure default database password detected: '{insecurePassword}'. Configure a unique database password via environment variables.");
            }
        }
    }
}
