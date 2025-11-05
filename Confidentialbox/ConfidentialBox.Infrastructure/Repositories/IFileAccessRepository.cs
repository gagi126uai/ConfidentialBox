namespace ConfidentialBox.Infrastructure.Repositories
{
    using FileAccessEntity = ConfidentialBox.Core.Entities.FileAccess;

    public interface IFileAccessRepository
    {
        Task<List<FileAccessEntity>> GetByFileIdAsync(int fileId);
        Task<int> GetTotalAccessesCountAsync();
        Task<int> GetUnauthorizedAccessesCountAsync();
        Task AddAsync(FileAccessEntity fileAccess);
    }
}