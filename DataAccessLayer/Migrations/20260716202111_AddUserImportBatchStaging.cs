using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserImportBatchStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "storage_locator",
                table: "user_import_batches",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "staging_locator",
                table: "user_import_batches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "user_import_batches"
                SET "storage_locator" = COALESCE("storage_locator", "staging_locator", '')
                WHERE "storage_locator" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "staging_locator",
                table: "user_import_batches");

            migrationBuilder.AlterColumn<string>(
                name: "storage_locator",
                table: "user_import_batches",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);


        }
    }
}
