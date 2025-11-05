using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Services;

public interface IFileStorageService
{
    Task StoreFileAsync(SharedFile file, byte[] encryptedBytes, CancellationToken cancellationToken = default);
    Task<byte[]> GetFileAsync(SharedFile file, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(SharedFile file, CancellationToken cancellationToken = default);
}
