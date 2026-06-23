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

            /* ==========================================================
               !!! WARNING !!! NO ROLLBACK
               ========================================================== */

            migrationBuilder.Sql("""
                ALTER FUNCTION public."Update_Timestamp_Function"() RENAME TO "update_timestamp";
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.update_timestamp()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                    BEGIN
                        NEW."updated_at" := now();
                        RETURN NEW;
                    END;
                $$;
                """);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "plans";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "plan_options";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "orders";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "subscriptions";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "payments";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "subjects";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "subject_memberships";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "subject_ai_configurations";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "global_ai_configurations";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chapters";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "documents";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "document_comments";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "parsed_sections";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chunks";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chat_sessions";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chat_messages";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chat_message_generation_settings";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "chat_message_generation_metrics";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "citations";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "test_questions";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "experiments";
                DROP TRIGGER IF EXISTS "UpdateTimestamp" ON "test_responses";
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "plans"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "plan_options"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "orders"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "subscriptions"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "payments"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "subjects"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "subject_memberships"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "subject_ai_configurations"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "global_ai_configurations"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chapters"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "documents"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "document_comments"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "parsed_sections"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chunks"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_sessions"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_messages"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "citations"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "test_questions"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "experiments"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "test_responses"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);

            /* ========================================================== */
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
