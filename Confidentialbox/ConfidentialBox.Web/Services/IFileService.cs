using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface IFileService
{
    Task<List<FileDto>> GetAllFilesAsync();
    Task<List<FileDto>> GetMyFilesAsync();
    Task<PagedResult<FileDto>> SearchFilesAsync(FileSearchRequest request);
    Task<FileDto?> GetFileByIdAsync(int id);
    Task<FileUploadResponse> UploadFileAsync(FileUploadRequest request);
    Task<FileDto?> AccessFileAsync(string shareLink, string? masterPassword);
    Task<FileContentResponse?> GetFileContentAsync(string shareLink, string? masterPassword);
    Task<bool> BlockFileAsync(int id, string reason);
    Task<bool> UnblockFileAsync(int id);
    Task<bool> DeleteFileAsync(int id);
    Task<List<FileAccessLogDto>> GetAccessLogsAsync(int fileId);
    Task<List<FileDto>> GetDeletedFilesAsync();
    Task<bool> RestoreFileAsync(int id);
    Task<bool> PurgeFileAsync(int id);
}