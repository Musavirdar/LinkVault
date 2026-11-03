using LinkVaultApi.Dtos;

namespace LinkVaultApi.Services;

public interface IDemoSeedService
{
    bool IsDemoModeEnabled();
    Task EnsureDemoWorkspaceSeededAsync(CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginDemoUserAsync(string role = "Admin", CancellationToken cancellationToken = default);
}
