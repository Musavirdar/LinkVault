using FluentAssertions;
using LinkVaultApi.Agent.Services;
using LinkVaultApi.Data;
using LinkVaultApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class SafeShareGuardTests
{
    private readonly LinkVaultDbContext _dbContext;
    private readonly Mock<IStorageProvider> _mockStorageProvider = new();
    private readonly Mock<IMetaLlamaClient> _mockLlamaClient = new();
    private readonly Mock<ILogger<SafeShareGuard>> _mockLogger = new();
    private readonly SafeShareGuard _guard;

    public SafeShareGuardTests()
    {
        var options = new DbContextOptionsBuilder<LinkVaultDbContext>()
            .UseInMemoryDatabase(databaseName: $"SafeShareDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new LinkVaultDbContext(options);

        _guard = new SafeShareGuard(
            _dbContext,
            _mockStorageProvider.Object,
            _mockLlamaClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ScanTextContentAsync_ExposedPostgresPassword_ReturnsBlocked()
    {
        // Arrange
        var content = "Host=dpg-xxx.render.com;Database=vault;Username=admin;Password=super_secret_password_123;";

        // Act
        var result = await _guard.ScanTextContentAsync(content, "env.render");

        // Assert
        result.Status.Should().Be("Blocked");
        result.CanShare.Should().BeFalse();
        result.RiskScore.Should().BeGreaterOrEqualTo(90);
        result.DetectedThreats.Should().Contain("PostgreSQL Connection String Password");
    }

    [Fact]
    public async Task ScanTextContentAsync_ExposedPrivateKey_ReturnsBlocked()
    {
        // Arrange
        var content = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA0...\n-----END RSA PRIVATE KEY-----";

        // Act
        var result = await _guard.ScanTextContentAsync(content, "server.key");

        // Assert
        result.Status.Should().Be("Blocked");
        result.CanShare.Should().BeFalse();
        result.RiskScore.Should().Be(100);
        result.DetectedThreats.Should().Contain("Cryptographic Private Key");
    }

    [Fact]
    public async Task ScanTextContentAsync_ExposedAwsKey_ReturnsBlocked()
    {
        // Arrange
        var content = "AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE";

        // Act
        var result = await _guard.ScanTextContentAsync(content, "aws.credentials");

        // Assert
        result.Status.Should().Be("Blocked");
        result.CanShare.Should().BeFalse();
        result.RiskScore.Should().BeGreaterOrEqualTo(80);
        result.DetectedThreats.Should().Contain("AWS Access Key ID");
    }

    [Fact]
    public async Task ScanTextContentAsync_CleanDocument_ReturnsCleanAndCanShare()
    {
        // Arrange
        var content = "Quarterly Business Review Q3 2026. All KPIs achieved with 24% revenue growth.";

        _mockLlamaClient
            .Setup(c => c.ModerateContentWithLlamaGuardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlamaGuardResult(true, 0, new List<string>(), "Clean document"));

        // Act
        var result = await _guard.ScanTextContentAsync(content, "Q3_Report.pdf");

        // Assert
        result.Status.Should().Be("Clean");
        result.CanShare.Should().BeTrue();
        result.RiskScore.Should().Be(0);
        result.DetectedThreats.Should().BeEmpty();
    }
}
