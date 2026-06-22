using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterAiConfigs_AddCitationExtraction_AlterCitation_AddOccurrenceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "citation_extraction_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "citation_extraction_temperature",
                table: "subject_ai_configurations",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "citation_extraction_prompt",
                table: "global_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "citation_extraction_temperature",
                table: "global_ai_configurations",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AlterColumn<string>(
                name: "quoted_text",
                table: "citations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "occurrence_index",
                table: "citations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "citation_extraction_prompt",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "citation_extraction_temperature",
                table: "chat_message_generation_settings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_message_generation_settings"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_message_generation_metrics"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "citation_extraction_prompt",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "citation_extraction_temperature",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "citation_extraction_prompt",
                table: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "citation_extraction_temperature",
                table: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "occurrence_index",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "citation_extraction_prompt",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "citation_extraction_temperature",
                table: "chat_message_generation_settings");

            migrationBuilder.AlterColumn<string>(
                name: "quoted_text",
                table: "citations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "update_timestamp" ON "chat_message_generation_settings";
                DROP TRIGGER IF EXISTS "update_timestamp" ON "chat_message_generation_metrics";
                """);
        }
    }
}
