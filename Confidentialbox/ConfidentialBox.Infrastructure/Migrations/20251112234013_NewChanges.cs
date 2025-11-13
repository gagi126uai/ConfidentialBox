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

            migrationBuilder.CreateTable(
                name: "SecurityAlertActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TargetFileId = table.Column<int>(type: "int", nullable: true),
                    StatusAfterAction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlertActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_SecurityAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SecurityAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityAlertActions_SharedFiles_TargetFileId",
                        column: x => x.TargetFileId,
                        principalTable: "SharedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_AlertId_CreatedAt",
                table: "SecurityAlertActions",
                columns: new[] { "AlertId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_CreatedByUserId",
                table: "SecurityAlertActions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_TargetFileId",
                table: "SecurityAlertActions",
                column: "TargetFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_TargetUserId",
                table: "SecurityAlertActions",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAlertActions");

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
