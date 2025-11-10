using System;

namespace ConfidentialBox.Core.Entities;

public class SecurityAlertAction
{
    public int Id { get; set; }

    public int AlertId { get; set; }
    public virtual SecurityAlert Alert { get; set; } = null!;

    /// <summary>
    /// Identifier of the action executed (BlockFile, BlockUser, MonitoringIncrease, Message, Note, StatusChange, etc.).
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Optional descriptive text shown in the action history.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// JSON metadata with additional details (payload, reason, etc.).
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByUserId { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public string? TargetUserId { get; set; }
    public virtual ApplicationUser? TargetUser { get; set; }

    public int? TargetFileId { get; set; }
    public virtual SharedFile? TargetFile { get; set; }

    /// <summary>
    /// Optional workflow status after executing the action.
    /// </summary>
    public string? StatusAfterAction { get; set; }
}
