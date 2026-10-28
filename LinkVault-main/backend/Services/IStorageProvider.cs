namespace LinkVaultApi.Services;

/// <summary>
/// Abstraction over storage backends (local disk, S3, MinIO, R2, etc.)
/// Allows swapping storage without changing business logic.
/// </summary>
public interface IStorageProvider
{
    /// <summary>Save a stream to the given path. Returns the final storage key.</summary>
    Task<string> SaveAsync(Stream stream, string path, CancellationToken ct = default);
    
    /// <summary>Retrieve a file as a readable stream.</summary>
    Task<Stream> GetAsync(string path, CancellationToken ct = default);
    
    /// <summary>Delete a file at the given path.</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);
    
    /// <summary>Check if a file exists at the given path.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    
    /// <summary>Get the total size in bytes of a file.</summary>
    Task<long> GetSizeAsync(string path, CancellationToken ct = default);
}
