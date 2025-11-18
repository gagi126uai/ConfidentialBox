using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMessageArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "UserMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserId_IsArchived_CreatedAt",
                table: "UserMessages",
                columns: new[] { "UserId", "IsArchived", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserMessages_UserId_IsArchived_CreatedAt",
                table: "UserMessages");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "UserMessages");
        }
    }
}
