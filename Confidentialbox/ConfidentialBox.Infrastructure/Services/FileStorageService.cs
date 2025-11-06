using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;
using Microsoft.Extensions.Options;
using System.Linq;

namespace ConfidentialBox.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly string _baseDirectory;

    public FileStorageService(IOptions<FileStorageSettings> options)
    {
        _settings = options.Value;
        if (!_settings.StoreInDatabase && !_settings.StoreOnFileSystem)
        {
            throw new InvalidOperationException("Debe habilitar el almacenamiento en base de datos o en sistema de archivos.");
        }

        _baseDirectory = Path.GetFullPath(_settings.FileSystemDirectory, AppContext.BaseDirectory);
        if (_settings.StoreOnFileSystem && !Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    public async Task StoreFileAsync(SharedFile file, byte[] encryptedBytes, CancellationToken cancellationToken = default)
    {
        if (encryptedBytes == null || encryptedBytes.Length == 0)
        {
            throw new ArgumentException("El archivo cifrado no puede estar vacío", nameof(encryptedBytes));
        }

        if (_settings.StoreInDatabase)
        {
            file.StoreInDatabase = true;
            file.EncryptedFileContent = encryptedBytes;
        }

        if (_settings.StoreOnFileSystem)
        {
            file.StoreOnFileSystem = true;

            var safeFileName = Path.GetFileName(file.EncryptedFileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = Guid.NewGuid().ToString("N");
            }

            var (targetPath, storagePath) = ResolveTargetPath(file, safeFileName);
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(targetPath, encryptedBytes, cancellationToken);
            file.StoragePath = storagePath;
        }
    }

    public async Task<byte[]> GetFileAsync(SharedFile file, CancellationToken cancellationToken = default)
    {
        if (file.StoreInDatabase && file.EncryptedFileContent != null)
        {
            return file.EncryptedFileContent;
        }

        if (file.StoreOnFileSystem)
        {
            if (string.IsNullOrWhiteSpace(file.StoragePath))
            {
                throw new InvalidOperationException("El archivo no tiene una ruta de almacenamiento válida.");
            }

            var fullPath = ResolveFullPath(file.StoragePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("No se encontró el archivo cifrado en el sistema de archivos.");
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }

        throw new InvalidOperationException("No hay un mecanismo de almacenamiento habilitado para este archivo.");
    }

    public Task DeleteFileAsync(SharedFile file, CancellationToken cancellationToken = default)
    {
        if (file.StoreOnFileSystem && !string.IsNullOrWhiteSpace(file.StoragePath))
        {
            var fullPath = ResolveFullPath(file.StoragePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            file.StoreOnFileSystem = false;
            file.StoragePath = null;
        }

        if (file.StoreInDatabase)
        {
            file.EncryptedFileContent = null;
            file.StoreInDatabase = false;
        }

        return Task.CompletedTask;
    }

    private (string fullPath, string storagePath) ResolveTargetPath(SharedFile file, string safeFileName)
    {
        if (!_settings.UseUserScopedDirectories || string.IsNullOrWhiteSpace(file.UploadedByUserId))
        {
            return (Path.Combine(_baseDirectory, safeFileName), safeFileName);
        }

        var userSegment = SanitizeSegment(file.UploadedByUserId);
        var storagePath = Path.Combine(userSegment, safeFileName);
        var fullPath = Path.Combine(_baseDirectory, storagePath);
        return (fullPath, storagePath.Replace('\\', '/'));
    }

    private string ResolveFullPath(string storagePath)
    {
        var safePath = storagePath.Replace('/', Path.DirectorySeparatorChar);
        safePath = safePath.Replace('\', Path.DirectorySeparatorChar);
        var combined = Path.Combine(_baseDirectory, safePath);
        var fullPath = Path.GetFullPath(combined);

        if (!fullPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ruta de almacenamiento inválida detectada.");
        }

        return fullPath;
    }

    private static string SanitizeSegment(string segment)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new char[segment.Length];
        var index = 0;
        foreach (var ch in segment)
        {
            if (invalidChars.Contains(ch) || ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
            {
                continue;
            }
            builder[index++] = ch;
        }

        if (index == 0)
        {
            return Guid.NewGuid().ToString("N");
        }

        return new string(builder, 0, index);
    }
}
