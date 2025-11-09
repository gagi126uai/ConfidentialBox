using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.IO;
using FileAccessEntity = ConfidentialBox.Core.Entities.FileAccess;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileAccessRepository _fileAccessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IShareLinkGenerator _linkGenerator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAISecurityService _aiSecurityService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IClientContextResolver _clientContextResolver;

    public FilesController(
        IFileRepository fileRepository,
        IFileAccessRepository fileAccessRepository,
        IAuditLogRepository auditLogRepository,
        IShareLinkGenerator linkGenerator,
        UserManager<ApplicationUser> userManager,
        IAISecurityService aiSecurityService,
        IFileStorageService fileStorageService,
        ISystemSettingsService systemSettingsService,
        IClientContextResolver clientContextResolver)
    {
        _fileRepository = fileRepository;
        _fileAccessRepository = fileAccessRepository;
        _auditLogRepository = auditLogRepository;
        _linkGenerator = linkGenerator;
        _userManager = userManager;
        _aiSecurityService = aiSecurityService;
        _fileStorageService = fileStorageService;
        _systemSettingsService = systemSettingsService;
        _clientContextResolver = clientContextResolver;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<FileDto>>> GetAll()
    {
        var files = await _fileRepository.GetAllAsync(includeDeleted: false);
        var fileDtos = files.Select(f => MapToDto(f)).ToList();
        return Ok(fileDtos);
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<FileDto>>> GetDeleted()
    {
        var files = await _fileRepository.GetDeletedAsync();
        return Ok(files.Select(MapToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileDto>> GetById(int id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(file));
    }

    [HttpGet("my-files")]
    public async Task<ActionResult<List<FileDto>>> GetMyFiles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var files = await _fileRepository.GetByUserIdAsync(userId);
        var fileDtos = files.Select(f => MapToDto(f)).ToList();
        return Ok(fileDtos);
    }

    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<FileDto>>> Search([FromBody] FileSearchRequest request)
    {
        var files = await _fileRepository.SearchAsync(
            request.SearchTerm,
            request.UploadedAfter,
            request.UploadedBefore,
            request.UploadedByUserId,
            request.IsBlocked,
            request.IsDeleted,
            request.PageNumber,
            request.PageSize);

        var totalCount = await _fileRepository.GetTotalCountAsync(
            request.SearchTerm,
            request.UploadedAfter,
            request.UploadedBefore,
            request.UploadedByUserId,
            request.IsBlocked,
            request.IsDeleted);

        return Ok(new PagedResult<FileDto>
        {
            Items = files.Select(f => MapToDto(f)).ToList(),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResponse>> Upload([FromBody] FileUploadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var storageSettings = await _systemSettingsService.GetFileStorageSettingsAsync(HttpContext.RequestAborted);
            var totalLimit = storageSettings.GetTotalStorageLimitBytes();
            if (totalLimit > 0)
            {
                var currentTotal = await _fileRepository.GetTotalStorageBytesAsync();
                if (currentTotal + request.FileSizeBytes > totalLimit)
                {
                    return Ok(new FileUploadResponse
                    {
                        Success = false,
                        ErrorMessage = "Se alcanzó el límite global de almacenamiento configurado."
                    });
                }
            }

            var perUserLimit = storageSettings.GetPerUserStorageLimitBytes();
            if (perUserLimit > 0)
            {
                var currentUserUsage = await _fileRepository.GetTotalStorageBytesByUserAsync(userId);
                if (currentUserUsage + request.FileSizeBytes > perUserLimit)
                {
                    return Ok(new FileUploadResponse
                    {
                        Success = false,
                        ErrorMessage = "Has superado tu cuota de almacenamiento disponible."
                    });
                }
            }

            var shareLink = _linkGenerator.GenerateUniqueLink();

            // Detectar si es PDF
            var isPDF = request.FileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.EncryptedContent))
            {
                return BadRequest("El contenido cifrado es obligatorio");
            }

            var encryptedBytes = Convert.FromBase64String(request.EncryptedContent);

            var file = new SharedFile
            {
                OriginalFileName = request.OriginalFileName,
                EncryptedFileName = request.EncryptedFileName,
                FileExtension = request.FileExtension,
                FileSizeBytes = request.FileSizeBytes,
                ShareLink = shareLink,
                EncryptionKey = request.EncryptionKey,
                MasterPassword = request.MasterPassword,
                ExpiresAt = request.ExpiresAt,
                MaxAccessCount = request.MaxAccessCount,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow,
                IsPDF = isPDF,
                // Configuración del visor seguro
                HasWatermark = isPDF && request.EnableWatermark,
                WatermarkText = string.IsNullOrWhiteSpace(request.WatermarkText) ? "CONFIDENTIAL" : request.WatermarkText,
                ScreenshotProtectionEnabled = isPDF && (request.ScreenshotProtection || request.EnableWatermark),
                PrintProtectionEnabled = isPDF && (request.PrintProtection || request.ScreenshotProtection),
                CopyProtectionEnabled = isPDF && (request.CopyProtection || request.ScreenshotProtection),
                AIMonitoringEnabled = isPDF && (request.AiMonitoring || request.ScreenshotProtection),
                MaxViewTimeMinutes = request.MaxViewTimeMinutes
            };

            await _fileStorageService.StoreFileAsync(file, encryptedBytes, HttpContext.RequestAborted);

            var savedFile = await _fileRepository.AddAsync(file);

            // Agregar permisos si se especificaron roles
            if (request.AllowedRoles.Any())
            {
                // Aquí agregarías la lógica de permisos por rol
            }

            // ANÁLISIS DE IA - Escanear archivo automáticamente
            try
            {
                var threatAnalysis = await _aiSecurityService.AnalyzeFileAsync(savedFile, userId);

                // Si el archivo es de alto riesgo, bloquearlo automáticamente
                if (threatAnalysis.ThreatScore >= 0.8)
                {
                    savedFile.IsBlocked = true;
                    savedFile.BlockReason = $"Bloqueado automáticamente por IA: {threatAnalysis.Recommendation}";
                    await _fileRepository.UpdateAsync(savedFile);
                }
            }
            catch (Exception aiEx)
            {
                // Log error pero no fallar el upload
                Console.WriteLine($"AI Analysis error: {aiEx.Message}");
            }

            // Registrar auditoría
            var clientContext = _clientContextResolver.Resolve(HttpContext);

            await _auditLogRepository.AddAsync(new AuditLog
            {
                UserId = userId,
                Action = "FileUploaded",
                EntityType = "SharedFile",
                EntityId = savedFile.Id.ToString(),
                NewValues = $"File: {request.OriginalFileName}",
                Timestamp = DateTime.UtcNow,
                IpAddress = clientContext.IpAddress,
                UserAgent = clientContext.UserAgent,
                DeviceName = clientContext.DeviceName,
                DeviceType = clientContext.DeviceType,
                OperatingSystem = clientContext.OperatingSystem,
                Browser = clientContext.Browser,
                Location = clientContext.Location,
                Latitude = clientContext.Latitude,
                Longitude = clientContext.Longitude
            });

            return Ok(new FileUploadResponse
            {
                Success = true,
                FileId = savedFile.Id,
                ShareLink = shareLink
            });
        }
        catch (Exception ex)
        {
            return Ok(new FileUploadResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpGet("access/{shareLink}")]
    [AllowAnonymous]
    public async Task<ActionResult<FileDto>> AccessFile(string shareLink, [FromQuery] string? masterPassword)
    {
        var file = await _fileRepository.GetByShareLinkAsync(shareLink);
        if (file == null)
        {
            return NotFound("Archivo no encontrado");
        }

        var requireMasterPassword = string.IsNullOrEmpty(file.MasterPassword) || !string.IsNullOrEmpty(masterPassword);
        var validation = await ValidateFileAccessAsync(
            file,
            masterPassword,
            incrementCounter: false,
            requireMasterPassword: requireMasterPassword);
        if (!validation.Success)
        {
            return BadRequest(validation.ErrorMessage);
        }

        await LogFileAccess(file.Id, "AccessGranted", true);

        return Ok(MapToDto(file));
    }

    [HttpGet("content/{shareLink}")]
    [AllowAnonymous]
    public async Task<ActionResult<FileContentResponse>> GetFileContent(string shareLink, [FromQuery] string? masterPassword)
    {
        var file = await _fileRepository.GetByShareLinkAsync(shareLink);
        if (file == null)
        {
            return NotFound();
        }

        var validation = await ValidateFileAccessAsync(
            file,
            masterPassword,
            incrementCounter: true,
            requireMasterPassword: true);
        if (!validation.Success)
        {
            return Ok(validation);
        }

        byte[] encryptedBytes;
        try
        {
            encryptedBytes = await _fileStorageService.GetFileAsync(file, HttpContext.RequestAborted);
        }
        catch (FileNotFoundException)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El archivo cifrado no está disponible en el almacenamiento"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = $"Error al recuperar el archivo: {ex.Message}"
            });
        }

        if (string.IsNullOrWhiteSpace(file.EncryptionKey))
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El archivo no tiene una clave de cifrado registrada"
            });
        }

        await LogFileAccess(file.Id, file.IsPDF ? "ContentRetrievedForViewer" : "ContentDownloaded", true);

        return Ok(new FileContentResponse
        {
            Success = true,
            FileName = file.OriginalFileName,
            FileExtension = file.FileExtension,
            EncryptedContent = Convert.ToBase64String(encryptedBytes),
            EncryptionKey = file.EncryptionKey,
            IsPdf = file.IsPDF,
            HasWatermark = file.HasWatermark,
            WatermarkText = file.WatermarkText,
            ScreenshotProtectionEnabled = file.ScreenshotProtectionEnabled,
            PrintProtectionEnabled = file.PrintProtectionEnabled,
            CopyProtectionEnabled = file.CopyProtectionEnabled,
            AimMonitoringEnabled = file.AIMonitoringEnabled,
            MaxViewTimeMinutes = file.MaxViewTimeMinutes
        });
    }

    [HttpPut("{id}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> BlockFile(int id, [FromBody] string reason)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        file.IsBlocked = true;
        file.BlockReason = reason;
        await _fileRepository.UpdateAsync(file);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId!,
            Action = "FileBlocked",
            EntityType = "SharedFile",
            EntityId = id.ToString(),
            NewValues = $"Reason: {reason}",
            Timestamp = DateTime.UtcNow,
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });

        return Ok();
    }

    [HttpPut("{id}/unblock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UnblockFile(int id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        file.IsBlocked = false;
        file.BlockReason = null;
        await _fileRepository.UpdateAsync(file);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId!,
            Action = "FileUnblocked",
            EntityType = "SharedFile",
            EntityId = id.ToString(),
            Timestamp = DateTime.UtcNow,
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verificar permisos
        if (file.UploadedByUserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        await _fileRepository.DeleteAsync(id);

        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId!,
            Action = "FileDeleted",
            EntityType = "SharedFile",
            EntityId = id.ToString(),
            OldValues = $"File: {file.OriginalFileName}",
            Timestamp = DateTime.UtcNow,
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });

        return Ok();
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Restore(int id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        if (!file.IsDeleted)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "El archivo no se encuentra en la papelera."
            });
        }

        await _fileRepository.RestoreAsync(id);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "FileRestored",
            EntityType = "SharedFile",
            EntityId = id.ToString(),
            Timestamp = DateTime.UtcNow,
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpDelete("{id}/purge")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Purge(int id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        if (!file.IsDeleted)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Solo es posible eliminar definitivamente archivos en la papelera."
            });
        }

        await _fileStorageService.DeleteFileAsync(file);
        await _fileRepository.PurgeAsync(id);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "FilePurged",
            EntityType = "SharedFile",
            EntityId = id.ToString(),
            OldValues = $"File: {file.OriginalFileName}",
            Timestamp = DateTime.UtcNow,
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("{id}/accesses")]
    public async Task<ActionResult<List<FileAccessLogDto>>> GetFileAccesses(int id)
    {
        var accesses = await _fileAccessRepository.GetByFileIdAsync(id);
        var dtos = accesses.Select(a => new FileAccessLogDto
        {
            Id = a.Id,
            AccessedAt = a.AccessedAt,
            WasAuthorized = a.WasAuthorized,
            Action = a.Action,
            AccessedByUserName = a.AccessedByUser != null ? $"{a.AccessedByUser.FirstName} {a.AccessedByUser.LastName}" : null,
            AccessedByIp = a.AccessedByIP,
            UserAgent = a.UserAgent,
            DeviceName = a.DeviceName,
            DeviceType = a.DeviceType,
            OperatingSystem = a.OperatingSystem,
            Browser = a.Browser,
            Location = a.Location,
            Latitude = a.Latitude,
            Longitude = a.Longitude
        }).ToList();

        return Ok(dtos);
    }

    private async Task LogFileAccess(int fileId, string action, bool wasAuthorized)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // requiere using System.Security.Claims

        var clientContext = _clientContextResolver.Resolve(HttpContext);

        await _fileAccessRepository.AddAsync(new FileAccessEntity
        {
            SharedFileId = fileId,
            AccessedByUserId = userId,
            AccessedByIP = clientContext.IpAddress,
            AccessedAt = DateTime.UtcNow,
            Action = action,
            WasAuthorized = wasAuthorized,
            UserAgent = clientContext.UserAgent,
            DeviceName = clientContext.DeviceName,
            DeviceType = clientContext.DeviceType,
            OperatingSystem = clientContext.OperatingSystem,
            Browser = clientContext.Browser,
            Location = clientContext.Location,
            Latitude = clientContext.Latitude,
            Longitude = clientContext.Longitude
        });
    }

    private async Task<FileContentResponse> ValidateFileAccessAsync(
        SharedFile file,
        string? masterPassword,
        bool incrementCounter,
        bool requireMasterPassword = true)
    {
        if (file.IsBlocked)
        {
            await LogFileAccess(file.Id, "AccessBlocked", false);
            return new FileContentResponse
            {
                Success = false,
                ErrorMessage = "Este archivo ha sido bloqueado"
            };
        }

        if (file.IsDeleted)
        {
            return new FileContentResponse
            {
                Success = false,
                ErrorMessage = "Archivo no disponible"
            };
        }

        if (file.ExpiresAt.HasValue && file.ExpiresAt.Value < DateTime.UtcNow)
        {
            await LogFileAccess(file.Id, "AccessExpired", false);
            return new FileContentResponse
            {
                Success = false,
                ErrorMessage = "Este archivo ha expirado"
            };
        }

        if (file.MaxAccessCount.HasValue && file.CurrentAccessCount >= file.MaxAccessCount.Value)
        {
            await LogFileAccess(file.Id, "AccessLimitReached", false);
            return new FileContentResponse
            {
                Success = false,
                ErrorMessage = "Se alcanzó el límite de accesos"
            };
        }

        if (requireMasterPassword && !string.IsNullOrEmpty(file.MasterPassword))
        {
            if (string.IsNullOrEmpty(masterPassword) || !string.Equals(file.MasterPassword, masterPassword, StringComparison.Ordinal))
            {
                await LogFileAccess(file.Id, "InvalidPassword", false);
                return new FileContentResponse
                {
                    Success = false,
                    ErrorMessage = "Contraseña incorrecta"
                };
            }
        }

        if (incrementCounter)
        {
            file.CurrentAccessCount++;
            await _fileRepository.UpdateAsync(file);
        }

        return new FileContentResponse { Success = true };
    }

    private FileDto MapToDto(SharedFile file)
    {
        return new FileDto
        {
            Id = file.Id,
            OriginalFileName = file.OriginalFileName,
            FileExtension = file.FileExtension,
            FileSizeBytes = file.FileSizeBytes,
            ShareLink = file.ShareLink,
            UploadedAt = file.UploadedAt,
            ExpiresAt = file.ExpiresAt,
            MaxAccessCount = file.MaxAccessCount,
            CurrentAccessCount = file.CurrentAccessCount,
            IsBlocked = file.IsBlocked,
            BlockReason = file.BlockReason,
            UploadedByUserName = $"{file.UploadedByUser?.FirstName} {file.UploadedByUser?.LastName}",
            HasMasterPassword = !string.IsNullOrEmpty(file.MasterPassword),
            IsPdf = file.IsPDF,
            HasWatermark = file.HasWatermark,
            ScreenshotProtectionEnabled = file.ScreenshotProtectionEnabled,
            AimMonitoringEnabled = file.AIMonitoringEnabled,
            IsDeleted = file.IsDeleted,
            DeletedAt = file.DeletedAt
        };
    }
}