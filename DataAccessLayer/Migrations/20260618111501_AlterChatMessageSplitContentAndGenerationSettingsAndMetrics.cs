using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterChatMessageSplitContentAndGenerationSettingsAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE citations
                RENAME CONSTRAINT fk_citations_chat_messages_message_id
                TO fk_citations_chat_messages_chat_message_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_subject_ai_configurations_subjects_subject_id",
                table: "subject_ai_configurations");

            migrationBuilder.DropIndex(
                name: "ix_subject_ai_configurations_subject_id",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "completion_tokens",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "llm_model",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "prompt_tokens",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "retrieval_time_ms",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "retrieved_chunk_count",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "temperature",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "time_to_first_token_ms",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "tokens_per_second",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "top_k",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "total_response_time_ms",
                table: "chat_messages");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "citations",
                newName: "chat_message_id");

            migrationBuilder.RenameIndex(
                name: "ix_citations_message_id",
                table: "citations",
                newName: "ix_citations_chat_message_id");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "chat_message_generation_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    retrieved_chunk_count = table.Column<int>(type: "integer", nullable: false),
                    context_chunk_count = table.Column<int>(type: "integer", nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    retrieval_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    time_to_first_token_ms = table.Column<long>(type: "bigint", nullable: false),
                    total_response_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    tokens_per_second = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_message_generation_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_message_generation_metrics_chat_messages_id",
                        column: x => x.id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_message_generation_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    top_k = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_chat_message_generation_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_message_generation_settings_chat_messages_id",
                        column: x => x.id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "fk_subject_ai_configurations_subjects_id",
                table: "subject_ai_configurations",
                column: "id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE citations
                RENAME CONSTRAINT fk_citations_chat_messages_chat_message_id
                TO fk_citations_chat_messages_message_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_subject_ai_configurations_subjects_id",
                table: "subject_ai_configurations");

            migrationBuilder.DropTable(
                name: "chat_message_generation_metrics");

            migrationBuilder.DropTable(
                name: "chat_message_generation_settings");

            migrationBuilder.RenameColumn(
                name: "chat_message_id",
                table: "citations",
                newName: "message_id");

            migrationBuilder.RenameIndex(
                name: "ix_citations_chat_message_id",
                table: "citations",
                newName: "ix_citations_message_id");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "subject_id",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "completion_tokens",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "llm_model",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prompt_tokens",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "retrieval_time_ms",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retrieved_chunk_count",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "temperature",
                table: "chat_messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "time_to_first_token_ms",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "tokens_per_second",
                table: "chat_messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "top_k",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "total_response_time_ms",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_subject_ai_configurations_subject_id",
                table: "subject_ai_configurations",
                column: "subject_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_subject_ai_configurations_subjects_subject_id",
                table: "subject_ai_configurations",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
