using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrainedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrainingSamples = table.Column<int>(type: "int", nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    Precision = table.Column<double>(type: "float", nullable: false),
                    Recall = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ModelPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresMFA = table.Column<bool>(type: "bit", nullable: false),
                    MFASecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicyValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePolicies_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EncryptedFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ShareLink = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MasterPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxAccessCount = table.Column<int>(type: "int", nullable: true),
                    CurrentAccessCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsPDF = table.Column<bool>(type: "bit", nullable: false),
                    HasWatermark = table.Column<bool>(type: "bit", nullable: false),
                    WatermarkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenshotProtectionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PrintProtectionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CopyProtectionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxViewTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    AIMonitoringEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedFileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    StoreInDatabase = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StoreOnFileSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedFiles_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AverageFilesPerDay = table.Column<double>(type: "float", nullable: false),
                    AverageFileSizeMB = table.Column<double>(type: "float", nullable: false),
                    TypicalActiveHoursStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    TypicalActiveHoursEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    CommonFileTypes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageSessionDuration = table.Column<double>(type: "float", nullable: false),
                    UnusualActivityCount = table.Column<int>(type: "int", nullable: false),
                    LastUnusualActivity = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RiskScore = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfileCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBehaviorProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FileAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedFileId = table.Column<int>(type: "int", nullable: false),
                    AccessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AccessedByIP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WasAuthorized = table.Column<bool>(type: "bit", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAccesses_AspNetUsers_AccessedByUserId",
                        column: x => x.AccessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FileAccesses_SharedFiles_SharedFileId",
                        column: x => x.SharedFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedFileId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanDownload = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FilePermissions_SharedFiles_SharedFileId",
                        column: x => x.SharedFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileScanResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedFileId = table.Column<int>(type: "int", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThreatScore = table.Column<double>(type: "float", nullable: false),
                    HasSuspiciousExtension = table.Column<bool>(type: "bit", nullable: false),
                    HasMaliciousPatterns = table.Column<bool>(type: "bit", nullable: false),
                    ExceedsSizeThreshold = table.Column<bool>(type: "bit", nullable: false),
                    UploadedOutsideBusinessHours = table.Column<bool>(type: "bit", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedFileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MalwareProbability = table.Column<double>(type: "float", nullable: false),
                    DataExfiltrationProbability = table.Column<double>(type: "float", nullable: false),
                    SocialEngineeringProbability = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileScanResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileScanResults_SharedFiles_SharedFileId",
                        column: x => x.SharedFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PDFViewerSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedFileId = table.Column<int>(type: "int", nullable: false),
                    ViewerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PageViewCount = table.Column<int>(type: "int", nullable: false),
                    CurrentPage = table.Column<int>(type: "int", nullable: false),
                    TotalPages = table.Column<int>(type: "int", nullable: false),
                    TotalViewTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ViewerIP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenshotAttempts = table.Column<int>(type: "int", nullable: false),
                    RapidPageChanges = table.Column<int>(type: "int", nullable: false),
                    PrintAttempts = table.Column<int>(type: "int", nullable: false),
                    CopyAttempts = table.Column<int>(type: "int", nullable: false),
                    WasBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadingPattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    SuspicionScore = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFViewerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PDFViewerSessions_AspNetUsers_ViewerUserId",
                        column: x => x.ViewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PDFViewerSessions_SharedFiles_SharedFileId",
                        column: x => x.SharedFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecycleBinItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedFileId = table.Column<int>(type: "int", nullable: false),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PermanentDeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRestored = table.Column<bool>(type: "bit", nullable: false),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecycleBinItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecycleBinItems_AspNetUsers_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecycleBinItems_SharedFiles_SharedFileId",
                        column: x => x.SharedFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedPattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActionTaken = table.Column<bool>(type: "bit", nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_SharedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PDFViewerEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    WasBlocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFViewerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PDFViewerEvents_PDFViewerSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PDFViewerSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_IsActive",
                table: "AIModels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_ModelName",
                table: "AIModels",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccesses_AccessedByUserId",
                table: "FileAccesses",
                column: "AccessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccesses_SharedFileId",
                table: "FileAccesses",
                column: "SharedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_RoleId",
                table: "FilePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_SharedFileId",
                table: "FilePermissions",
                column: "SharedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileScanResults_IsSuspicious",
                table: "FileScanResults",
                column: "IsSuspicious");

            migrationBuilder.CreateIndex(
                name: "IX_FileScanResults_ScannedAt",
                table: "FileScanResults",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileScanResults_SharedFileId",
                table: "FileScanResults",
                column: "SharedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerEvents_EventType",
                table: "PDFViewerEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerEvents_SessionId_Timestamp",
                table: "PDFViewerEvents",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerEvents_Timestamp",
                table: "PDFViewerEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_IsSuspicious",
                table: "PDFViewerSessions",
                column: "IsSuspicious");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_SessionId",
                table: "PDFViewerSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_SharedFileId_StartedAt",
                table: "PDFViewerSessions",
                columns: new[] { "SharedFileId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_StartedAt",
                table: "PDFViewerSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_ViewerUserId",
                table: "PDFViewerSessions",
                column: "ViewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFViewerSessions_WasBlocked",
                table: "PDFViewerSessions",
                column: "WasBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_RecycleBinItems_DeletedByUserId",
                table: "RecycleBinItems",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecycleBinItems_SharedFileId",
                table: "RecycleBinItems",
                column: "SharedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePolicies_RoleId",
                table: "RolePolicies",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_DetectedAt",
                table: "SecurityAlerts",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_FileId",
                table: "SecurityAlerts",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_IsReviewed",
                table: "SecurityAlerts",
                column: "IsReviewed");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_IsReviewed_Severity_DetectedAt",
                table: "SecurityAlerts",
                columns: new[] { "IsReviewed", "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Severity",
                table: "SecurityAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UserId",
                table: "SecurityAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category_Key",
                table: "SystemSettings",
                columns: new[] { "Category", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserId",
                table: "SystemSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_ShareLink",
                table: "SharedFiles",
                column: "ShareLink",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_UploadedByUserId",
                table: "SharedFiles",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorProfiles_RiskScore",
                table: "UserBehaviorProfiles",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorProfiles_UserId",
                table: "UserBehaviorProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIModels");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FileAccesses");

            migrationBuilder.DropTable(
                name: "FilePermissions");

            migrationBuilder.DropTable(
                name: "FileScanResults");

            migrationBuilder.DropTable(
                name: "PDFViewerEvents");

            migrationBuilder.DropTable(
                name: "RecycleBinItems");

            migrationBuilder.DropTable(
                name: "RolePolicies");

            migrationBuilder.DropTable(
                name: "SecurityAlerts");

            migrationBuilder.DropTable(
                name: "UserBehaviorProfiles");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[SystemSettings]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_SystemSettings_AspNetUsers_UpdatedByUserId'
          AND parent_object_id = OBJECT_ID(N'[SystemSettings]')
    )
    BEGIN
        ALTER TABLE [SystemSettings] DROP CONSTRAINT [FK_SystemSettings_AspNetUsers_UpdatedByUserId];
    END;

    DROP TABLE [SystemSettings];
END
");

            migrationBuilder.DropTable(
                name: "PDFViewerSessions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "SharedFiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
