using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStorageConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "file_path",
                table: "documents",
                newName: "storage_locator");

            migrationBuilder.AlterColumn<string>(
                name: "storage_locator",
                table: "documents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropColumn(
                name: "file_name",
                table: "documents");

            migrationBuilder.AddColumn<string>(
                name: "staging_locator",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "storage_method",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AlterColumn<int>(
                name: "storage_method",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.CreateTable(
                name: "subject_storage_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    storage_method = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_storage_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_subject_storage_configurations_subjects_id",
                        column: x => x.id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                BEFORE UPDATE ON "subject_storage_configurations"
                FOR EACH ROW
                EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subject_storage_configurations");

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "documents"
                SET "file_name" = COALESCE(
                    NULLIF(regexp_replace(COALESCE("storage_locator", "staging_locator", ''), '^.*[\\/]', ''), ''),
                    NULLIF("original_file_name", ''),
                    "id"::text
                );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.Sql("""
                UPDATE "documents"
                SET "storage_locator" = COALESCE("storage_locator", "staging_locator", '')
                WHERE "storage_locator" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "staging_locator",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "storage_method",
                table: "documents");

            migrationBuilder.AlterColumn<string>(
                name: "storage_locator",
                table: "documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "storage_locator",
                table: "documents",
                newName: "file_path");
        }
    }
}
