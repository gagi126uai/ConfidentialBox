using Microsoft.EntityFrameworkCore.Migrations;

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSystemSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[SystemSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [SystemSettings]
    (
        [Id] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        [Category] NVARCHAR(128) NOT NULL,
        [Key] NVARCHAR(128) NOT NULL,
        [Value] NVARCHAR(MAX) NOT NULL,
        [IsSensitive] BIT NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        [UpdatedByUserId] NVARCHAR(450) NULL
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_SystemSettings_Category_Key'
      AND [object_id] = OBJECT_ID(N'[SystemSettings]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_SystemSettings_Category_Key]
        ON [SystemSettings] ([Category], [Key]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_SystemSettings_UpdatedByUserId'
      AND [object_id] = OBJECT_ID(N'[SystemSettings]')
)
BEGIN
    CREATE INDEX [IX_SystemSettings_UpdatedByUserId]
        ON [SystemSettings] ([UpdatedByUserId]);
END
");

            migrationBuilder.Sql(@"
DECLARE @utcNow DATETIME2(7) = SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'Security' AND [Key] = N'UserRegistrationEnabled'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (N'Security', N'UserRegistrationEnabled', N'True', 0, @utcNow, NULL);
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'Security' AND [Key] = N'TokenLifetimeHours'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (N'Security', N'TokenLifetimeHours', N'12', 0, @utcNow, NULL);
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'Storage' AND [Key] = N'StoreInDatabase'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (N'Storage', N'StoreInDatabase', N'True', 0, @utcNow, NULL);
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'Storage' AND [Key] = N'StoreOnFileSystem'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (N'Storage', N'StoreOnFileSystem', N'False', 0, @utcNow, NULL);
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'Storage' AND [Key] = N'FileSystemDirectory'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (N'Storage', N'FileSystemDirectory', N'SecureStorage', 0, @utcNow, NULL);
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'EmailNotifications' AND [Key] = N'Channels'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (
        N'EmailNotifications',
        N'Channels',
        N'{""SecurityAlertRecipients"": """, ""PasswordRecoveryRecipients"": """, ""UserInvitationRecipients"": """}',
        0,
        @utcNow,
        NULL
    );
END

IF NOT EXISTS (
    SELECT 1 FROM [SystemSettings]
    WHERE [Category] = N'AI' AND [Key] = N'ScoringSettings'
)
BEGIN
    INSERT INTO [SystemSettings] ([Category], [Key], [Value], [IsSensitive], [UpdatedAt], [UpdatedByUserId])
    VALUES (
        N'AI',
        N'ScoringSettings',
        N'{""HighRiskThreshold"":0.7,""SuspiciousThreshold"":0.5,""SuspiciousExtensionScore"":0.3,""LargeFileScore"":0.2,""OutsideBusinessHoursScore"":0.15,""UnusualUploadsScore"":0.25,""UnusualFileSizeScore"":0.2,""OutsideHoursBehaviorScore"":0.2,""UnusualActivityIncrement"":0.1,""MalwareProbabilityWeight"":0.4,""DataExfiltrationWeight"":0.3,""BusinessHoursStart"":7,""BusinessHoursEnd"":20,""UploadAnomalyMultiplier"":3.0,""FileSizeAnomalyMultiplier"":2.0,""MaxFileSizeMB"":100,""MalwareSuspiciousExtensionWeight"":0.5,""MalwareCrackKeywordWeight"":0.3,""MalwareKeygenKeywordWeight"":0.3,""MalwareExecutableWeight"":0.2,""DataExfiltrationLargeFileMB"":50,""DataExfiltrationHugeFileMB"":100,""DataExfiltrationLargeFileWeight"":0.3,""DataExfiltrationHugeFileWeight"":0.3,""DataExfiltrationArchiveWeight"":0.2,""DataExfiltrationOffHoursWeight"":0.2,""RecommendationBlockThreshold"":0.8,""RecommendationReviewThreshold"":0.6,""RecommendationMonitorThreshold"":0.4,""RiskLevelHighThreshold"":0.7,""RiskLevelMediumThreshold"":0.4,""SuspiciousExtensions"": ["".exe"","".bat"","".cmd"","".ps1"","".vbs"","".js""]}',
        0,
        @utcNow,
        NULL
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[SystemSettings]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [SystemSettings];
END
");
        }
    }
}
