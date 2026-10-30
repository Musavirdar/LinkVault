using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using LinkVaultApi.Agent.Hubs;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Agent.Services;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class AgentHardeningTests
{
    private readonly LinkVaultDbContext _dbContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mock<IMetaLlamaClient> _mockLlamaClient = new();
    private readonly Mock<ISafeShareGuard> _mockSafeShareGuard = new();
    private readonly Mock<IStorageProvider> _mockStorageProvider = new();
    private readonly Mock<IHubContext<AgentHub>> _mockHubContext = new();
    private readonly Mock<ILogger<VaultAgentService>> _mockLogger = new();
    private readonly VaultAgentService _service;

    private readonly Guid _orgAId = Guid.NewGuid();
    private readonly Guid _orgBId = Guid.NewGuid();
    private readonly Guid _userAId = Guid.NewGuid();
    private readonly Guid _userBId = Guid.NewGuid();

    public AgentHardeningTests()
    {
        var dbName = $"AgentHardeningDb_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddDbContext<LinkVaultDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddSingleton(_mockLlamaClient.Object);
        services.AddSingleton(_mockSafeShareGuard.Object);
        services.AddSingleton(_mockStorageProvider.Object);
        services.AddSingleton(_mockHubContext.Object);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _dbContext = serviceProvider.GetRequiredService<LinkVaultDbContext>();

        // Seed Organization A & B
        _dbContext.Organizations.Add(new Organization { Id = _orgAId, Name = "Org A", Slug = "org-a" });
        _dbContext.Organizations.Add(new Organization { Id = _orgBId, Name = "Org B", Slug = "org-b" });

        // Seed User A (in Org A) and User B (in Org A)
        _dbContext.Users.Add(new User { Id = _userAId, OrganizationId = _orgAId, Email = "usera@example.com", Username = "user-a" });
        _dbContext.Users.Add(new User { Id = _userBId, OrganizationId = _orgAId, Email = "userb@example.com", Username = "user-b" });
        _dbContext.SaveChanges();

        // Safe Share Guard mock defaults
        _mockSafeShareGuard
            .Setup(s => s.ScanResourceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafeShareScanResponse
            {
                ResourceId = Guid.NewGuid(),
                RiskScore = 0,
                Status = "Clean",
                CanShare = true,
                Message = "File scanned: clean."
            });

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
            _scopeFactory,
            _mockLogger.Object);
    }

    private static ClaimsPrincipal CreateUserPrincipal(Guid userId, Guid orgId, string role = "Admin", Dictionary<string, string>? customClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("organizationId", orgId.ToString()),
            new(ClaimTypes.Role, role)
        };

        if (customClaims != null)
        {
            foreach (var kvp in customClaims)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value));
            }
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task ApprovePlanAsync_DoubleApproval_Returns409ConflictException()
    {
        // Arrange: Plan created and in AwaitingApproval status
        var plan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            UserGoal = "Archive documents",
            Status = PlanStatus.AwaitingApproval,
            Summary = "Plan for archiving",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        var caller = CreateUserPrincipal(_userAId, _orgAId, role: "Member");

        // Act 1: First approval succeeds
        var firstResponse = await _service.ApprovePlanAsync(plan.Id, caller);
        firstResponse.Status.Should().Be("Executing");

        // Act 2: Second approval on the same plan must fail with InvalidOperationException (409 Conflict)
        var act2 = () => _service.ApprovePlanAsync(plan.Id, caller);
        await act2.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in AwaitingApproval status*");
    }

    [Fact]
    public async Task ApprovePlanAsync_ApprovingAnotherUsersPlanByNonAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Plan belongs to User B
        var plan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userBId,
            UserGoal = "Confidential operations",
            Status = PlanStatus.AwaitingApproval,
            Summary = "Confidential plan",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // User A (non-admin) attempts to approve User B's plan
        var userACaller = CreateUserPrincipal(_userAId, _orgAId, role: "Member");

        // Act & Assert: Must throw UnauthorizedAccessException (mapped to 403 Forbid)
        var act = () => _service.ApprovePlanAsync(plan.Id, userACaller);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*do not have permission to approve this plan*");
    }

    [Fact]
    public async Task ExecutePlanSteps_ViewerRoleTryingMoveAndShare_FailsWithClaimDenials()
    {
        // Arrange: Viewer role with canUpdate=False and canShare=False
        var viewerClaims = new Dictionary<string, string>
        {
            ["canUpload"] = "False",
            ["canUpdate"] = "False",
            ["canShare"] = "False",
            ["canDelete"] = "False"
        };
        var viewerCaller = CreateUserPrincipal(_userAId, _orgAId, role: "Viewer", viewerClaims);

        var existingResource = new Resource
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            Name = "Quarterly_Results.pdf",
            CreatedById = _userAId,
            StoragePath = "orgA/Quarterly_Results.pdf",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Resources.Add(existingResource);

        // 1. Test move_resource denial for Viewer
        var movePlan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            Status = PlanStatus.Executing,
            Summary = "Test move"
        };
        movePlan.Steps.Add(new PlanStep
        {
            StepNumber = 1,
            ToolName = "move_resource",
            ParametersJson = JsonSerializer.Serialize(new { resourceId = existingResource.Id.ToString() }),
            Status = StepStatus.Pending
        });
        _dbContext.AgentActionPlans.Add(movePlan);
        await _dbContext.SaveChangesAsync();

        await _service.ExecutePlanStepsAsync(movePlan.Id, _orgAId, _userAId, viewerCaller);

        _dbContext.ChangeTracker.Clear();
        var updatedMovePlan = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == movePlan.Id);
        updatedMovePlan.Status.Should().Be(PlanStatus.Failed);
        updatedMovePlan.Steps.First().Status.Should().Be(StepStatus.Failed);
        updatedMovePlan.Steps.First().ErrorMessage.Should().Contain("canUpdate");

        // 2. Test create_share_link denial for Viewer
        var sharePlan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            Status = PlanStatus.Executing,
            Summary = "Test share"
        };
        sharePlan.Steps.Add(new PlanStep
        {
            StepNumber = 1,
            ToolName = "create_share_link",
            ParametersJson = JsonSerializer.Serialize(new { resourceId = existingResource.Id.ToString(), expiryDays = 7 }),
            Status = StepStatus.Pending
        });
        _dbContext.AgentActionPlans.Add(sharePlan);
        await _dbContext.SaveChangesAsync();

        await _service.ExecutePlanStepsAsync(sharePlan.Id, _orgAId, _userAId, viewerCaller);

        _dbContext.ChangeTracker.Clear();
        var updatedSharePlan = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == sharePlan.Id);
        updatedSharePlan.Status.Should().Be(PlanStatus.Failed);
        updatedSharePlan.Steps.First().Status.Should().Be(StepStatus.Failed);
        updatedSharePlan.Steps.First().ErrorMessage.Should().Contain("canShare");
    }

    [Fact]
    public async Task ExecutePlanSteps_MissingResourceId_FailsStepWithoutFallback()
    {
        // Arrange: Seed an existing resource in Org A that unsafe code would have picked as fallback
        var decoyResource = new Resource
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            Name = "Decoy_Resource.pdf",
            DirectoryId = null,
            CreatedById = _userAId,
            StoragePath = "orgA/decoy.pdf",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Resources.Add(decoyResource);

        var caller = CreateUserPrincipal(_userAId, _orgAId, role: "Admin");

        var plan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            Status = PlanStatus.Executing,
            Summary = "Plan with missing resourceId"
        };
        // Omit resourceId entirely in arguments
        plan.Steps.Add(new PlanStep
        {
            StepNumber = 1,
            ToolName = "move_resource",
            ParametersJson = "{}",
            Status = StepStatus.Pending
        });
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ExecutePlanStepsAsync(plan.Id, _orgAId, _userAId, caller);

        // Assert: Step must fail immediately and decoy resource must NOT have been moved
        _dbContext.ChangeTracker.Clear();
        var updatedPlan = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == plan.Id);
        updatedPlan.Status.Should().Be(PlanStatus.Failed);
        updatedPlan.Steps.First().Status.Should().Be(StepStatus.Failed);
        updatedPlan.Steps.First().ErrorMessage.Should().Contain("resourceId");

        var verifiedDecoy = await _dbContext.Resources.FindAsync(decoyResource.Id);
        verifiedDecoy!.DirectoryId.Should().BeNull();
    }

    [Fact]
    public async Task ExecutePlanSteps_ResourceFromAnotherOrganization_FailsTenantCheck()
    {
        // Arrange: Resource belongs to Org B
        var foreignResource = new Resource
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgBId,
            Name = "Secret_OrgB_File.pdf",
            DirectoryId = null,
            CreatedById = Guid.NewGuid(),
            StoragePath = "orgB/secret.pdf",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Resources.Add(foreignResource);
        await _dbContext.SaveChangesAsync();

        // Caller is Admin of Org A
        var caller = CreateUserPrincipal(_userAId, _orgAId, role: "Admin");

        var plan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            Status = PlanStatus.Executing,
            Summary = "Attempt cross-tenant access"
        };
        plan.Steps.Add(new PlanStep
        {
            StepNumber = 1,
            ToolName = "move_resource",
            ParametersJson = JsonSerializer.Serialize(new { resourceId = foreignResource.Id.ToString() }),
            Status = StepStatus.Pending
        });
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ExecutePlanStepsAsync(plan.Id, _orgAId, _userAId, caller);

        // Assert: Cross-tenant access blocked with tenant boundary error
        _dbContext.ChangeTracker.Clear();
        var updatedPlan = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == plan.Id);
        updatedPlan.Status.Should().Be(PlanStatus.Failed);
        updatedPlan.Steps.First().Status.Should().Be(StepStatus.Failed);
        updatedPlan.Steps.First().ErrorMessage.Should().Contain("Resource not found or belongs to another organization");
    }

    [Fact]
    public async Task RejectPlanAsync_WhenAwaitingApproval_SetsStatusToRejectedAndSkipsSteps()
    {
        // Arrange
        var plan = new AgentActionPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgAId,
            UserId = _userAId,
            UserGoal = "Reject me",
            Status = PlanStatus.AwaitingApproval,
            Summary = "Plan to be rejected"
        };
        plan.Steps.Add(new PlanStep
        {
            StepNumber = 1,
            ToolName = "create_directory",
            ParametersJson = "{\"name\":\"Folder\"}",
            Status = StepStatus.Pending
        });
        _dbContext.AgentActionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        var caller = CreateUserPrincipal(_userAId, _orgAId, role: "Member");

        // Act
        var response = await _service.RejectPlanAsync(plan.Id, caller);

        // Assert
        response.Status.Should().Be("Rejected");
        var updatedPlan = await _dbContext.AgentActionPlans.Include(p => p.Steps).FirstAsync(p => p.Id == plan.Id);
        updatedPlan.Status.Should().Be(PlanStatus.Rejected);
        updatedPlan.Steps.First().Status.Should().Be(StepStatus.Skipped);

        // Second reject must throw InvalidOperationException
        var secondAct = () => _service.RejectPlanAsync(plan.Id, caller);
        await secondAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in AwaitingApproval status*");
    }

    [Fact]
    public void ValidateToolArguments_EnforcesPerToolSchemaAnd10StepCapping()
    {
        // 1. create_directory: missing name
        var (val1, err1) = MetaLlamaClient.ValidateToolArguments("create_directory", "{}");
        val1.Should().BeFalse();
        err1.Should().Contain("name");

        // 2. move_resource: missing resourceId
        var (val2, err2) = MetaLlamaClient.ValidateToolArguments("move_resource", "{}");
        val2.Should().BeFalse();
        err2.Should().Contain("resourceId");

        // 3. create_share_link: invalid expiryDays
        var (val3, err3) = MetaLlamaClient.ValidateToolArguments("create_share_link", JsonSerializer.Serialize(new
        {
            resourceId = Guid.NewGuid().ToString(),
            expiryDays = 500 // > 365
        }));
        val3.Should().BeFalse();
        err3.Should().Contain("expiryDays");

        // 4. Valid create_share_link
        var (val4, err4) = MetaLlamaClient.ValidateToolArguments("create_share_link", JsonSerializer.Serialize(new
        {
            resourceId = Guid.NewGuid().ToString(),
            expiryDays = 14
        }));
        val4.Should().BeTrue();
        err4.Should().BeNull();
    }
}
