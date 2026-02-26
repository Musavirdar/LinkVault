namespace LinkVault.Api.Services.Interfaces;

/// <summary>
/// Abstraction over file storage back-ends.
/// Default implementation: LocalStorageProvider (stores under /uploads).
/// Override by registering a different implementation (S3StorageProvider, AzureBlobStorageProvider).
/// </summary>
public interface IStorageProvider
{
    /// <summary>Save a stream and return the storage path/key (opaque identifier).</summary>
    Task<string> SaveAsync(Stream stream, string fileName, string contentType);

    /// <summary>Open a read stream for the given storage path/key.</summary>
    Task<Stream> OpenReadAsync(string storagePath);

    /// <summary>Permanently delete a stored object.</summary>
    Task DeleteAsync(string storagePath);

    /// <summary>Returns true if the object exists.</summary>
    Task<bool> ExistsAsync(string storagePath);
}
