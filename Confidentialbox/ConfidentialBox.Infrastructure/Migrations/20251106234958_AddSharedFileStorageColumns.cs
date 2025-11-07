using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    public partial class AddSharedFileStorageColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedFileContent",
                table: "SharedFiles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "SharedFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StoreInDatabase",
                table: "SharedFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StoreOnFileSystem",
                table: "SharedFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedFileContent",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "StoreInDatabase",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "StoreOnFileSystem",
                table: "SharedFiles");
        }
    }
}
