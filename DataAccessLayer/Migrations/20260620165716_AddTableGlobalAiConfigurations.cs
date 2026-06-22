using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTableGlobalAiConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "top_k",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<double>(
                name: "temperature",
                table: "subject_ai_configurations",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<string>(
                name: "system_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "max_history_messages",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "max_context_chunks",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "llm_model",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "embedding_model",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "chunking_strategy",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<double>(
                name: "similarity_threshold",
                table: "subject_ai_configurations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "similarity_threshold",
                table: "chat_message_generation_settings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "global_ai_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chunking_strategy = table.Column<string>(type: "text", nullable: false),
                    embedding_model = table.Column<string>(type: "text", nullable: false),
                    top_k = table.Column<int>(type: "integer", nullable: false),
                    similarity_threshold = table.Column<double>(type: "double precision", nullable: false),
                    llm_model = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    max_context_chunks = table.Column<int>(type: "integer", nullable: false),
                    max_history_messages = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_global_ai_configurations", x => x.id);
                });

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "global_ai_configurations"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "similarity_threshold",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "similarity_threshold",
                table: "chat_message_generation_settings");

            migrationBuilder.AlterColumn<int>(
                name: "top_k",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "temperature",
                table: "subject_ai_configurations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "system_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "max_history_messages",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "max_context_chunks",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "llm_model",
                table: "subject_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "embedding_model",
                table: "subject_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "chunking_strategy",
                table: "subject_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
