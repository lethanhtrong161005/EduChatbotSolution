using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterChatMessage_AddColRawContent_AddTblCitationOccurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "occurrence_index",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "quoted_text",
                table: "citations");

            migrationBuilder.AddColumn<string>(
                name: "raw_content",
                table: "chat_messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "citation_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    citation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_index = table.Column<int>(type: "integer", nullable: false),
                    supporting_quote = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_citation_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_citation_occurrences_citations_citation_id",
                        column: x => x.citation_id,
                        principalTable: "citations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_citation_occurrences_citation_id",
                table: "citation_occurrences",
                column: "citation_id");

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "citation_occurrences"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "citation_occurrences");

            migrationBuilder.DropColumn(
                name: "raw_content",
                table: "chat_messages");

            migrationBuilder.AddColumn<int>(
                name: "occurrence_index",
                table: "citations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "quoted_text",
                table: "citations",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "update_timestamp" ON "citation_occurrences";
                """);
        }
    }
}
