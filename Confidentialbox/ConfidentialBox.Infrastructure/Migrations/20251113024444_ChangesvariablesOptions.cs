using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangesvariablesOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedByUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
     name: "UserMessages",
     columns: table => new
     {
         Id = table.Column<int>(type: "int", nullable: false)
             .Annotation("SqlServer:Identity", "1, 1"),
         UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
         SenderId = table.Column<string>(type: "nvarchar(450)", nullable: true),
         Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
         Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
         CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
         IsRead = table.Column<bool>(type: "bit", nullable: false),
         RequiresResponse = table.Column<bool>(type: "bit", nullable: false)
     },
     constraints: table =>
     {
         table.PrimaryKey("PK_UserMessages", x => x.Id);
         table.ForeignKey(
             name: "FK_UserMessages_AspNetUsers_SenderId",
             column: x => x.SenderId,
             principalTable: "AspNetUsers",
             principalColumn: "Id",
             onDelete: ReferentialAction.SetNull);
         table.ForeignKey(
             name: "FK_UserMessages_AspNetUsers_UserId",
             column: x => x.UserId,
             principalTable: "AspNetUsers",
             principalColumn: "Id",
             onDelete: ReferentialAction.Restrict); // <-- CAMBIO CLAVE
                                                    // (o ReferentialAction.NoAction si tu EF lo permite)
     });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BlockedByUserId",
                table: "AspNetUsers",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_SenderId",
                table: "UserMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserId_IsRead_CreatedAt",
                table: "UserMessages",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedByUserId",
                table: "UserNotifications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BlockedByUserId",
                table: "AspNetUsers",
                column: "BlockedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BlockedByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserMessages");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BlockedByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BlockedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BlockedByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "AspNetUsers");
        }
    }
}
