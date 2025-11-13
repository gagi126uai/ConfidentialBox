using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SharedFile> SharedFiles { get; set; }
    public DbSet<Core.Entities.FileAccess> FileAccesses { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<FilePermission> FilePermissions { get; set; }
    public DbSet<RolePolicy> RolePolicies { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<RecycleBinItem> RecycleBinItems { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<UserMessage> UserMessages { get; set; }


    // AI Security Module
    public DbSet<SecurityAlert> SecurityAlerts { get; set; }
    public DbSet<SecurityAlertAction> SecurityAlertActions { get; set; }
    public DbSet<UserBehaviorProfile> UserBehaviorProfiles { get; set; }
    public DbSet<FileScanResult> FileScanResults { get; set; }
    public DbSet<AIModel> AIModels { get; set; }

    // PDF Viewer Secure Module
    public DbSet<PDFViewerSession> PDFViewerSessions { get; set; }
    public DbSet<PDFViewerEvent> PDFViewerEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SharedFile
        builder.Entity<SharedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.EncryptedFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ShareLink).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.ShareLink).IsUnique();
            entity.Property(e => e.StoragePath).HasMaxLength(1024);
            entity.Property(e => e.EncryptedFileContent).HasColumnType("varbinary(max)");

            entity.HasOne(e => e.UploadedByUser)
                  .WithMany(u => u.UploadedFiles)
                  .HasForeignKey(e => e.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Opcional: si existe un RowVersion
            // entity.Property(e => e.RowVersion).IsRowVersion();
            // Opcional: soft delete
            // builder.Entity<SharedFile>().HasQueryFilter(f => !f.IsDeleted);
        });

        // FileAccess
        builder.Entity<Core.Entities.FileAccess>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.SharedFile)
                  .WithMany(f => f.FileAccesses)
                  .HasForeignKey(e => e.SharedFileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AccessedByUser)
                  .WithMany(u => u.FileAccesses)
                  .HasForeignKey(e => e.AccessedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.DeviceName).HasMaxLength(256);
            entity.Property(e => e.DeviceType).HasMaxLength(64);
            entity.Property(e => e.OperatingSystem).HasMaxLength(128);
            entity.Property(e => e.Browser).HasMaxLength(128);
            entity.Property(e => e.Location).HasMaxLength(256);

            // Si existe AccessedAt:
            // entity.HasIndex(e => new { e.SharedFileId, e.AccessedAt });
        });

        // AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Action);

            entity.Property(e => e.DeviceName).HasMaxLength(256);
            entity.Property(e => e.DeviceType).HasMaxLength(64);
            entity.Property(e => e.OperatingSystem).HasMaxLength(128);
            entity.Property(e => e.Browser).HasMaxLength(128);
            entity.Property(e => e.Location).HasMaxLength(256);
        });

        // FilePermission
        builder.Entity<FilePermission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.SharedFile)
                  .WithMany(f => f.FilePermissions)
                  .HasForeignKey(e => e.SharedFileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.FilePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RolePolicy
        builder.Entity<RolePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePolicies)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Si tenés (RoleId, PolicyName) únicos:
            // entity.HasIndex(e => new { e.RoleId, e.PolicyName }).IsUnique();
        });

        // RecycleBinItem
        builder.Entity<RecycleBinItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SharedFile)
                  .WithMany()
                  .HasForeignKey(e => e.SharedFileId)
                  .OnDelete(DeleteBehavior.Restrict);
            // entity.HasIndex(e => e.DeletedAt); // si existe
        });

        // ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.BlockReason).HasMaxLength(512);
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.BlockedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            // entity.HasIndex(e => e.Email).IsUnique(); // si no lo maneja Identity
        });

        // ApplicationRole
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        builder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.Severity).HasMaxLength(32);
            entity.Property(e => e.Link).HasMaxLength(512);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        });

        builder.Entity<UserMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Body).HasMaxLength(4000);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                  .WithMany()
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        });

        // SecurityAlert
        builder.Entity<SecurityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Severity).HasMaxLength(32);
            entity.Property(e => e.AlertType).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.Property(e => e.Verdict).HasMaxLength(128);
            // entity.Property(e => e.ConfidenceScore).HasPrecision(5,2); // si es decimal

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.File)
                  .WithMany()
                  .HasForeignKey(e => e.FileId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.IsReviewed);

            // Índice compuesto recomendado para consultas:
            entity.HasIndex(e => new { e.IsReviewed, e.Severity, e.DetectedAt });

            // Opcional: índice filtrado en SQL Server
            // entity.HasIndex(e => e.IsReviewed).HasFilter("[IsReviewed] = 0");
        });

        builder.Entity<SecurityAlertAction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActionType).HasMaxLength(64);
            entity.Property(e => e.StatusAfterAction).HasMaxLength(32);

            entity.HasOne(e => e.Alert)
                  .WithMany()
                  .HasForeignKey(e => e.AlertId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetUser)
                  .WithMany()
                  .HasForeignKey(e => e.TargetUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetFile)
                  .WithMany()
                  .HasForeignKey(e => e.TargetFileId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.AlertId, e.CreatedAt });
        });

        // UserBehaviorProfile
        builder.Entity<UserBehaviorProfile>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.RiskScore);
            entity.Property(e => e.MonitoringNotes).HasMaxLength(1024);
            entity.Property(e => e.MonitoringLevel).HasDefaultValue(1);
            // entity.Property(e => e.RiskScore).HasPrecision(5,2);
        });

        // FileScanResult
        builder.Entity<FileScanResult>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.SharedFile)
                  .WithMany()
                  .HasForeignKey(e => e.SharedFileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ScannedAt);
            entity.HasIndex(e => e.IsSuspicious);
        });

        // AIModel
        builder.Entity<AIModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelName).HasMaxLength(128);
            entity.HasIndex(e => e.ModelName);
            entity.HasIndex(e => e.IsActive);
            // entity.Property(e => e.Version).HasMaxLength(32);
        });

        // PDFViewerSession
        builder.Entity<PDFViewerSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.SessionId).IsUnique();

            entity.HasOne(e => e.SharedFile)
                  .WithMany()
                  .HasForeignKey(e => e.SharedFileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ViewerUser)
                  .WithMany()
                  .HasForeignKey(e => e.ViewerUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.IsSuspicious);
            entity.HasIndex(e => e.WasBlocked);

            // Acceso rápido por archivo/fecha
            entity.HasIndex(e => new { e.SharedFileId, e.StartedAt });
        });

        // PDFViewerEvent
        builder.Entity<PDFViewerEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType).HasMaxLength(64);
            // Si la relación inversa existe:
            // entity.HasOne(e => e.Session)
            //       .WithMany(s => s.Events)
            //       .HasForeignKey(e => e.SessionId)
            //       .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => new { e.SessionId, e.Timestamp });
        });

        builder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Value).IsRequired();
            entity.HasIndex(e => new { e.Category, e.Key }).IsUnique();
        });
    }

}