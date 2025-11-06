using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfidentialBox.Infrastructure.Migrations
{
    public partial class AddMissingFileStorageColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('SharedFiles', 'EncryptedFileContent') IS NULL
    ALTER TABLE SharedFiles ADD EncryptedFileContent varbinary(max) NULL;
IF COL_LENGTH('SharedFiles', 'StoreInDatabase') IS NULL
BEGIN
    ALTER TABLE SharedFiles ADD StoreInDatabase bit NOT NULL CONSTRAINT DF_SharedFiles_StoreInDatabase DEFAULT 0;
END
IF COL_LENGTH('SharedFiles', 'StoreOnFileSystem') IS NULL
BEGIN
    ALTER TABLE SharedFiles ADD StoreOnFileSystem bit NOT NULL CONSTRAINT DF_SharedFiles_StoreOnFileSystem DEFAULT 0;
END
IF COL_LENGTH('SharedFiles', 'StoragePath') IS NULL
    ALTER TABLE SharedFiles ADD StoragePath nvarchar(1024) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('DF_SharedFiles_StoreOnFileSystem', 'D') IS NOT NULL
    ALTER TABLE SharedFiles DROP CONSTRAINT DF_SharedFiles_StoreOnFileSystem;
IF OBJECT_ID('DF_SharedFiles_StoreInDatabase', 'D') IS NOT NULL
    ALTER TABLE SharedFiles DROP CONSTRAINT DF_SharedFiles_StoreInDatabase;
IF COL_LENGTH('SharedFiles', 'StoragePath') IS NOT NULL
    ALTER TABLE SharedFiles DROP COLUMN StoragePath;
IF COL_LENGTH('SharedFiles', 'StoreOnFileSystem') IS NOT NULL
    ALTER TABLE SharedFiles DROP COLUMN StoreOnFileSystem;
IF COL_LENGTH('SharedFiles', 'StoreInDatabase') IS NOT NULL
    ALTER TABLE SharedFiles DROP COLUMN StoreInDatabase;
IF COL_LENGTH('SharedFiles', 'EncryptedFileContent') IS NOT NULL
    ALTER TABLE SharedFiles DROP COLUMN EncryptedFileContent;
");
        }
    }
}
