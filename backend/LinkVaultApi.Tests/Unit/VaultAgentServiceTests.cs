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

public class VaultAgentServiceTests
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

    public VaultAgentServiceTests()
    {
        var options = new DbContextOptionsBuilder<LinkVaultDbContext>()
            .UseInMemoryDatabase(databaseName: $"VaultAgentDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new LinkVaultDbContext(options);

        // Seed organization and user
        _dbContext.Organizations.Add(new Organization
        {
            Id = _orgId,
            Name = "Acme Corp",
            Slug = "acme-corp"
        });

        _dbContext.Users.Add(new User
        {
            Id = _userId,
            OrganizationId = _orgId,
            Email = "agent-user@example.com",
            Username = "agent-user"
        });
        _dbContext.SaveChanges();

        // Mock Hub Clients
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
    public async Task GeneratePlanAsync_DecomposesGoalIntoAwaitingApprovalPlan()
    {
        // Arrange
        var user = CreateUserPrincipal(_userId, _orgId);
        var request = new CreatePlanRequest
        {
            UserGoal = "Organize my invoices into an Invoices folder and share the latest invoice for 7 days"
        };

        var dummyResourceId = Guid.NewGuid();
        var fakeToolCalls = new List<LlamaToolCall>
        {
            new("create_directory", "{\"name\":\"Invoices\"}", "Create folder 'Invoices'"),
            new("move_resource", $"{{\"resourceId\":\"{dummyResourceId}\"}}", "Move invoices"),
            new("create_share_link", $"{{\"resourceId\":\"{dummyResourceId}\",\"expiryDays\":7}}", "Share invoice for 7 days")
        };

        _mockLlamaClient
            .Setup(c => c.GenerateToolPlanAsync(request.UserGoal, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlamaPlanResult("Proposed 3 steps to organize invoices", fakeToolCalls));

        // Act
        var response = await _service.GeneratePlanAsync(request, user);

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be("AwaitingApproval");
        response.RequiresUserApproval.Should().BeTrue();
        response.Steps.Should().HaveCount(3);
        response.Steps[0].ToolName.Should().Be("create_directory");
        response.Steps[1].ToolName.Should().Be("move_resource");
        response.Steps[2].ToolName.Should().Be("create_share_link");

        var planInDb = await _dbContext.AgentActionPlans
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == response.PlanId);

        planInDb.Should().NotBeNull();
        planInDb!.Status.Should().Be(PlanStatus.AwaitingApproval);
        planInDb.Steps.Should().HaveCount(3);
    }

    [Fact]
    public async Task GeneratePlanAsync_UnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Principal with no claims
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var request = new CreatePlanRequest { UserGoal = "Organize uploads" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GeneratePlanAsync(request, user));
    }

    [Fact]
    public async Task RejectPlanAsync_TransitionsStatusToRejectedAndSkipsPendingSteps()
    {
        // Arrange
        var user = CreateUserPrincipal(_userId, _orgId);
        var plan = new AgentActionPlan
        {
            OrganizationId = _orgId,
            UserId = _userId,
            UserGoal = "Organize uploads",
            Status = PlanStatus.AwaitingApproval,
            Steps = new List<PlanStep>
            {
                new() { StepNumber = 1, ToolName = "move_resource", Status = StepStatus.Pending }
            }
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _service.RejectPlanAsync(plan.Id, user);

        // Assert
        response.Status.Should().Be("Rejected");

        var updated = await _dbContext.AgentActionPlans
            .Include(p => p.Steps)
            .FirstAsync(p => p.Id == plan.Id);

        updated.Status.Should().Be(PlanStatus.Rejected);
        updated.Steps.First().Status.Should().Be(StepStatus.Skipped);
    }

    [Fact]
    public async Task ApprovePlanAsync_TransitionsStatusToExecuting()
    {
        // Arrange
        var user = CreateUserPrincipal(_userId, _orgId);
        var plan = new AgentActionPlan
        {
            OrganizationId = _orgId,
            UserId = _userId,
            UserGoal = "Organize uploads",
            Status = PlanStatus.AwaitingApproval,
            Steps = new List<PlanStep>
            {
                new() { StepNumber = 1, ToolName = "create_directory", ParametersJson = "{\"name\":\"Test\"}", Status = StepStatus.Pending }
            }
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _service.ApprovePlanAsync(plan.Id, user);

        // Assert
        response.Status.Should().Be("Executing");
        var updated = await _dbContext.AgentActionPlans.FindAsync(plan.Id);
        updated!.Status.Should().Be(PlanStatus.Executing);
        updated.ApprovedAt.Should().NotBeNull();
    }
}

