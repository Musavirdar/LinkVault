using FluentAssertions;
using LinkVaultApi.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class StartupValidationTests
{
    [Fact]
    public void Validate_WithMissingJwtKey_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT secret key is missing*");
    }

    [Fact]
    public void Validate_WithShortJwtKey_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "short-key-16-bytes!",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be at least 256 bits*");
    }

    [Fact]
    public void Validate_WithDefaultInsecurePlaceholderKey_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "your-super-secret-key-change-this-in-production-123",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insecure default JWT secret placeholder*");
    }

    [Fact]
    public void Validate_WithMissingConnectionString_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "a-very-strong-and-secure-jwt-secret-key-that-exceeds-32-bytes!"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Database connection string*");
    }

    [Fact]
    public void Validate_WithCommittedDefaultKeyOverride_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "linkvault-default-key-override-with-jwt-secret-in-production-env",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insecure default JWT secret placeholder*");
    }

    [Fact]
    public void Validate_WithCommittedDevSecretKey_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "lv-dev-secret-key-a8f3b9c2e1d4a7f6b5c8e2d1f4a9b3c7",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insecure default JWT secret placeholder*");
    }

    [Fact]
    public void Validate_WithCommittedDockerPlaceholderKey_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "your-super-secret-key-that-is-at-least-32-characters-long-for-jwt-signing",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insecure default JWT secret placeholder*");
    }

    [Fact]
    public void Validate_WithCommittedPostgresPassword_ShouldThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "a-very-strong-and-secure-jwt-secret-key-that-exceeds-32-bytes!",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=linkvault;Password=linkvault123"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insecure default database password detected*");
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "a-very-strong-and-secure-jwt-secret-key-that-exceeds-32-bytes!",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=secure_fresh_password"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().NotThrow();
    }
}
