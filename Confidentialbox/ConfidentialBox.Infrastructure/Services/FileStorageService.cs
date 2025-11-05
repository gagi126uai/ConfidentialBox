using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;
using Microsoft.Extensions.Options;

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

            var targetPath = Path.Combine(_baseDirectory, safeFileName);
            await File.WriteAllBytesAsync(targetPath, encryptedBytes, cancellationToken);
            file.StoragePath = safeFileName;
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

            var safePath = Path.GetFileName(file.StoragePath);
            var fullPath = Path.Combine(_baseDirectory, safePath);
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
            var safePath = Path.GetFileName(file.StoragePath);
            var fullPath = Path.Combine(_baseDirectory, safePath);
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
}
