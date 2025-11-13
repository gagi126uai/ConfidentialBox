using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangesinOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityAlertActions_AlertId",
                table: "SecurityAlertActions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlertActions_AlertId",
                table: "SecurityAlertActions",
                column: "AlertId");
        }
    }
}
