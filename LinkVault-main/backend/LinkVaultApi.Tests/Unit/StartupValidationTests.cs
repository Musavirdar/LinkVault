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
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "a-very-strong-and-secure-jwt-secret-key-that-exceeds-32-bytes!",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault;Username=postgres;Password=test"
            })
            .Build();

        var act = () => StartupValidator.Validate(config);
        act.Should().NotThrow();
    }
}
