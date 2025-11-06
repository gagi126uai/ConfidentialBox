using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditController(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await _auditLogRepository.GetAllAsync(pageNumber, pageSize);
        var totalCount = await _auditLogRepository.GetTotalCountAsync();

        var logDtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserName = $"{l.User.FirstName} {l.User.LastName}",
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Timestamp = l.Timestamp,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            DeviceName = l.DeviceName,
            DeviceType = l.DeviceType,
            OperatingSystem = l.OperatingSystem,
            Browser = l.Browser,
            Location = l.Location,
            Latitude = l.Latitude,
            Longitude = l.Longitude
        }).ToList();

        return Ok(new PagedResult<AuditLogDto>
        {
            Items = logDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }
}