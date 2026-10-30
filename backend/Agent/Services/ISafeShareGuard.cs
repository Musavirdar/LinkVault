using LinkVaultApi.Agent.Models;

namespace LinkVaultApi.Agent.Services;

public interface ISafeShareGuard
{
    Task<SafeShareScanResponse> ScanResourceAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default);
    Task<SafeShareScanResponse> ScanTextContentAsync(string content, string fileName = "file.txt", CancellationToken cancellationToken = default);
}
