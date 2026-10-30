using System.Security.Claims;
using FluentAssertions;
using LinkVaultApi.Agent.Hubs;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Agent.Services;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class AgentApprovalAndAuditTests
{
    private readonly LinkVaultDbContext _dbContext;
    private readonly Mock<IMetaLlamaClient> _mockLlamaClient = new();
    private readonly Mock<ISafeShareGuard> _mockSafeShareGuard = new();
    private readonly Mock<IStorageProvider> _mockStorageProvider = new();
    private readonly Mock<IHubContext<AgentHub>> _mockHubContext = new();
    private readonly Mock<ILogger<VaultAgentService>> _mockLogger = new();
    private readonly VaultAgentService _service;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public AgentApprovalAndAuditTests()
    {
        var options = new DbContextOptionsBuilder<LinkVaultDbContext>()
            .UseInMemoryDatabase(databaseName: $"AgentApprovalDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new LinkVaultDbContext(options);

        _dbContext.Organizations.Add(new Organization
        {
            Id = _orgId,
            Name = "Audit Corp",
            Slug = "audit-corp"
        });

        _dbContext.Users.Add(new User
        {
            Id = _userId,
            OrganizationId = _orgId,
            Email = "audit-user@example.com",
            Username = "audit-user"
        });
        _dbContext.SaveChanges();

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new VaultAgentService(
            _dbContext,
            _mockLlamaClient.Object,
            _mockSafeShareGuard.Object,
            _mockStorageProvider.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    private ClaimsPrincipal CreateUserPrincipal(Guid userId, Guid orgId, string role = "Admin")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("organizationId", orgId.ToString()),
            new(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task ApprovePlanAsync_NonOwnerNonAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Plan belongs to other user
        var otherUserId = Guid.NewGuid();
        var plan = new AgentActionPlan
        {
            OrganizationId = _orgId,
            UserId = otherUserId,
            UserGoal = "Confidential files",
            Status = PlanStatus.AwaitingApproval
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        var normalUser = CreateUserPrincipal(_userId, _orgId, role: "Viewer");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ApprovePlanAsync(plan.Id, normalUser));
    }

    [Fact]
    public async Task RejectPlanAsync_PreventsExecutionAndSkipsSteps()
    {
        // Arrange
        var user = CreateUserPrincipal(_userId, _orgId);
        var plan = new AgentActionPlan
        {
            OrganizationId = _orgId,
            UserId = _userId,
            UserGoal = "Move invoices",
            Status = PlanStatus.AwaitingApproval,
            Steps = new List<PlanStep>
            {
                new() { StepNumber = 1, ToolName = "create_directory", ParametersJson = "{\"name\":\"Invoices\"}", Status = StepStatus.Pending },
                new() { StepNumber = 2, ToolName = "move_resource", ParametersJson = "{}", Status = StepStatus.Pending }
            }
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        var res = await _service.RejectPlanAsync(plan.Id, user);

        // Assert
        res.Status.Should().Be("Rejected");
        var updated = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == plan.Id);
        updated.Status.Should().Be(PlanStatus.Rejected);
        updated.Steps.Should().OnlyContain(s => s.Status == StepStatus.Skipped);

        // No new directories should have been created
        var dirs = await _dbContext.Directories.Where(d => d.OrganizationId == _orgId).ToListAsync();
        dirs.Should().BeEmpty();
    }
}
