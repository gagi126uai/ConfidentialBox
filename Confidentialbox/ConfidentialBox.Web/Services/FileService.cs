using ConfidentialBox.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class FileService : IFileService
{
    private readonly HttpClient _httpClient;

    public FileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FileDto>> GetAllFilesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<FileDto>>("api/files") ?? new List<FileDto>();
    }

    public async Task<List<FileDto>> GetMyFilesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<FileDto>>("api/files/my-files") ?? new List<FileDto>();
    }

    public async Task<PagedResult<FileDto>> SearchFilesAsync(FileSearchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/files/search", request);
        return await response.Content.ReadFromJsonAsync<PagedResult<FileDto>>() ?? new PagedResult<FileDto>();
    }

    public async Task<FileDto?> GetFileByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<FileDto>($"api/files/{id}");
    }

    public async Task<FileUploadResponse> UploadFileAsync(FileUploadRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/files/upload", request);
        return await response.Content.ReadFromJsonAsync<FileUploadResponse>()
            ?? new FileUploadResponse { Success = false, ErrorMessage = "Error desconocido" };
    }

    public async Task<FileDto?> AccessFileAsync(string shareLink, string? masterPassword)
    {
        var url = $"api/files/access/{shareLink}";
        if (!string.IsNullOrEmpty(masterPassword))
        {
            url += $"?masterPassword={Uri.EscapeDataString(masterPassword)}";
        }

        try
        {
            return await _httpClient.GetFromJsonAsync<FileDto>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<FileContentResponse?> GetFileContentAsync(string shareLink, string? masterPassword)
    {
        var url = $"api/files/content/{shareLink}";
        if (!string.IsNullOrEmpty(masterPassword))
        {
            url += $"?masterPassword={Uri.EscapeDataString(masterPassword)}";
        }

        try
        {
            return await _httpClient.GetFromJsonAsync<FileContentResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> BlockFileAsync(int id, string reason)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/files/{id}/block", reason);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnblockFileAsync(int id)
    {
        var response = await _httpClient.PutAsync($"api/files/{id}/unblock", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteFileAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/files/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<FileAccessLogDto>> GetAccessLogsAsync(int fileId)
    {
        return await _httpClient.GetFromJsonAsync<List<FileAccessLogDto>>($"api/files/{fileId}/accesses")
            ?? new List<FileAccessLogDto>();
    }

    public async Task<List<FileDto>> GetDeletedFilesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<FileDto>>("api/files/deleted") ?? new List<FileDto>();
    }

    public async Task<bool> RestoreFileAsync(int id)
    {
        var response = await _httpClient.PostAsync($"api/files/{id}/restore", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PurgeFileAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/files/{id}/purge");
        return response.IsSuccessStatusCode;
    }
}
