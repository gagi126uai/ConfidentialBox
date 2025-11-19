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
            migrationBuilder.Sql(@"IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_UserMessages_UserId_IsArchived_CreatedAt'
                      AND object_id = OBJECT_ID('[UserMessages]')
                )
                DROP INDEX [IX_UserMessages_UserId_IsArchived_CreatedAt] ON [UserMessages];");

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
            migrationBuilder.Sql(@"IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_UserMessages_UserId_IsArchived_CreatedAt'
                      AND object_id = OBJECT_ID('[UserMessages]')
                )
                DROP INDEX [IX_UserMessages_UserId_IsArchived_CreatedAt] ON [UserMessages];");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "UserMessages");
        }
    }
}
