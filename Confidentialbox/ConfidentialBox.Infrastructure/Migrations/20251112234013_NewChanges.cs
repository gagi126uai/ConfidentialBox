using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonitoringLevel",
                table: "UserBehaviorProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "MonitoringLevelUpdatedAt",
                table: "UserBehaviorProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonitoringNotes",
                table: "UserBehaviorProfiles",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                table: "SecurityAlerts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SecurityAlerts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Verdict",
                table: "SecurityAlerts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClipboardEvents",
                table: "PDFViewerSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FullscreenExitEvents",
                table: "PDFViewerSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityLossEvents",
                table: "PDFViewerSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WindowBlurEvents",
                table: "PDFViewerSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SecurityAlertActions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SecurityAlertActions] (
        [Id] INT NOT NULL IDENTITY,
        [AlertId] INT NOT NULL,
        [ActionType] NVARCHAR(64) NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [Metadata] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedByUserId] NVARCHAR(450) NULL,
        [TargetUserId] NVARCHAR(450) NULL,
        [TargetFileId] INT NULL,
        [StatusAfterAction] NVARCHAR(32) NULL,
        CONSTRAINT [PK_SecurityAlertActions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SecurityAlertActions_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_SecurityAlertActions_AspNetUsers_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_SecurityAlertActions_SecurityAlerts_AlertId] FOREIGN KEY ([AlertId]) REFERENCES [SecurityAlerts]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SecurityAlertActions_SharedFiles_TargetFileId] FOREIGN KEY ([TargetFileId]) REFERENCES [SharedFiles]([Id]) ON DELETE SET NULL
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SecurityAlertActions_AlertId_CreatedAt'
      AND object_id = OBJECT_ID(N'[dbo].[SecurityAlertActions]')
)
BEGIN
    CREATE INDEX [IX_SecurityAlertActions_AlertId_CreatedAt]
        ON [dbo].[SecurityAlertActions]([AlertId], [CreatedAt]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SecurityAlertActions_CreatedByUserId'
      AND object_id = OBJECT_ID(N'[dbo].[SecurityAlertActions]')
)
BEGIN
    CREATE INDEX [IX_SecurityAlertActions_CreatedByUserId]
        ON [dbo].[SecurityAlertActions]([CreatedByUserId]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SecurityAlertActions_TargetUserId'
      AND object_id = OBJECT_ID(N'[dbo].[SecurityAlertActions]')
)
BEGIN
    CREATE INDEX [IX_SecurityAlertActions_TargetUserId]
        ON [dbo].[SecurityAlertActions]([TargetUserId]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SecurityAlertActions_TargetFileId'
      AND object_id = OBJECT_ID(N'[dbo].[SecurityAlertActions]')
)
BEGIN
    CREATE INDEX [IX_SecurityAlertActions_TargetFileId]
        ON [dbo].[SecurityAlertActions]([TargetFileId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SecurityAlertActions]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[SecurityAlertActions];
END
");

            migrationBuilder.DropColumn(
                name: "MonitoringLevel",
                table: "UserBehaviorProfiles");

            migrationBuilder.DropColumn(
                name: "MonitoringLevelUpdatedAt",
                table: "UserBehaviorProfiles");

            migrationBuilder.DropColumn(
                name: "MonitoringNotes",
                table: "UserBehaviorProfiles");

            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "Verdict",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "ClipboardEvents",
                table: "PDFViewerSessions");

            migrationBuilder.DropColumn(
                name: "FullscreenExitEvents",
                table: "PDFViewerSessions");

            migrationBuilder.DropColumn(
                name: "VisibilityLossEvents",
                table: "PDFViewerSessions");

            migrationBuilder.DropColumn(
                name: "WindowBlurEvents",
                table: "PDFViewerSessions");
        }
    }
}
