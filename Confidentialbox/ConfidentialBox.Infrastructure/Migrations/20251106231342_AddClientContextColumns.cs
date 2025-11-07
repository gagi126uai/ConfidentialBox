using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    public partial class AddClientContextColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "AuditLogs",
                type: "nvarchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "AuditLogs",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "AuditLogs",
                type: "nvarchar(64)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AuditLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AuditLogs",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AuditLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "AuditLogs",
                type: "nvarchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "FileAccesses",
                type: "nvarchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "FileAccesses",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "FileAccesses",
                type: "nvarchar(64)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FileAccesses",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "FileAccesses",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FileAccesses",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "FileAccesses",
                type: "nvarchar(128)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FileAccesses");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "FileAccesses");
        }
    }
}
