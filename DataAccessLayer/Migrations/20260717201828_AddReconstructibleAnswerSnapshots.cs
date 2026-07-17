using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddReconstructibleAnswerSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_citations_chunks_chunk_id",
                table: "citations");

            migrationBuilder.DropForeignKey(
                name: "fk_test_responses_test_questions_test_question_id",
                table: "test_responses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts");

            migrationBuilder.AlterColumn<long>(name: "prompt_tokens", table: "chat_session_title_generation_metrics", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "completion_tokens", table: "chat_session_title_generation_metrics", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "prompt_tokens", table: "chat_message_generation_metrics", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "completion_tokens", table: "chat_message_generation_metrics", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "prompt_tokens", table: "test_responses", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "completion_tokens", table: "test_responses", type: "bigint", nullable: true, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AddColumn<string>(name: "embedding_provider", table: "experiment_configuration_snapshots", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "llm_provider", table: "experiment_configuration_snapshots", type: "text", nullable: false, defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "context_text",
                table: "test_response_contexts",
                newName: "chunk_text");

            migrationBuilder.RenameColumn(
                name: "context_index",
                table: "test_response_contexts",
                newName: "retrieval_rank");

            migrationBuilder.RenameIndex(
                name: "ix_test_response_contexts_test_response_id_context_index",
                table: "test_response_contexts",
                newName: "ix_test_response_contexts_test_response_id_retrieval_rank");

            migrationBuilder.RenameColumn(
                name: "context_text",
                table: "chat_message_contexts",
                newName: "chunk_text");

            migrationBuilder.RenameColumn(
                name: "context_index",
                table: "chat_message_contexts",
                newName: "retrieval_rank");

            migrationBuilder.RenameIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts",
                newName: "ix_chat_message_contexts_chat_message_id_retrieval_rank");

            migrationBuilder.AlterColumn<int>(
                name: "test_question_id",
                table: "test_responses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "dataset_key",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dataset_name",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dataset_version",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ground_truth",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_difficulty",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_external_id",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_language",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_generated_answer",
                table: "test_responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reconstruction_completeness",
                table: "test_responses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source_question_id",
                table: "test_responses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "chunk_id",
                table: "test_response_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "chunk_index",
                table: "test_response_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_file_name",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_title",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "end_page_number",
                table: "test_response_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_section_title",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prompt_order",
                table: "test_response_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "similarity_score",
                table: "test_response_contexts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_chunk_id",
                table: "test_response_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                table: "test_response_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_subject_id",
                table: "test_response_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "start_page_number",
                table: "test_response_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_section_title",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_code",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_name",
                table: "test_response_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "was_included_in_prompt",
                table: "test_response_contexts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "reconstruction_completeness",
                table: "experiments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "chunk_id",
                table: "citations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "retrieval_snapshot_id",
                table: "citations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reconstruction_completeness",
                table: "chat_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "embedding_provider",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "llm_provider",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reasoning_effort",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reasoning_output",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "chunk_id",
                table: "chat_message_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "chunk_index",
                table: "chat_message_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_file_name",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_title",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "end_page_number",
                table: "chat_message_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_section_title",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prompt_order",
                table: "chat_message_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "similarity_score",
                table: "chat_message_contexts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_chunk_id",
                table: "chat_message_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                table: "chat_message_contexts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_subject_id",
                table: "chat_message_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "start_page_number",
                table: "chat_message_contexts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_section_title",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_code",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_name",
                table: "chat_message_contexts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "was_included_in_prompt",
                table: "chat_message_contexts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "chat_message_request_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_order = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_message_request_messages", x => x.id);
                    table.CheckConstraint("ck_chat_message_request_messages_message_order", "message_order >= 1");
                    table.ForeignKey(
                        name: "fk_chat_message_request_messages_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_message_subject_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_order = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<int>(type: "integer", nullable: true),
                    source_subject_id = table.Column<int>(type: "integer", nullable: false),
                    subject_code = table.Column<string>(type: "text", nullable: false),
                    subject_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_message_subject_snapshots", x => x.id);
                    table.CheckConstraint("ck_chat_message_subject_snapshots_subject_order", "subject_order >= 1");
                    table.ForeignKey(
                        name: "fk_chat_message_subject_snapshots_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_message_subject_snapshots_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "test_response_request_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_order = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_response_request_messages", x => x.id);
                    table.CheckConstraint("ck_test_response_request_messages_message_order", "message_order >= 1");
                    table.ForeignKey(
                        name: "fk_test_response_request_messages_test_responses_test_response",
                        column: x => x.test_response_id,
                        principalTable: "test_responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE "chat_message_contexts"
                SET "was_included_in_prompt" = TRUE,
                    "prompt_order" = "retrieval_rank";

                UPDATE "test_response_contexts"
                SET "was_included_in_prompt" = TRUE,
                    "prompt_order" = "retrieval_rank";

                UPDATE "test_responses" AS response
                SET "source_question_id" = question."id",
                    "question_external_id" = question."external_id",
                    "question_language" = question."language",
                    "question_difficulty" = question."difficulty",
                    "question" = question."question",
                    "ground_truth" = question."ground_truth"
                FROM "test_questions" AS question
                WHERE response."test_question_id" = question."id";
                """);

            migrationBuilder.CreateIndex(
                name: "ix_test_response_contexts_chunk_id",
                table: "test_response_contexts",
                column: "chunk_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_response_contexts_test_response_id_prompt_order",
                table: "test_response_contexts",
                columns: new[] { "test_response_id", "prompt_order" },
                unique: true,
                filter: "\"prompt_order\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_response_contexts_prompt_order",
                table: "test_response_contexts",
                sql: "(was_included_in_prompt = TRUE AND prompt_order >= 1) OR (was_included_in_prompt = FALSE AND prompt_order IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_response_contexts_retrieval_rank",
                table: "test_response_contexts",
                sql: "retrieval_rank >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_citations_retrieval_snapshot_id",
                table: "citations",
                column: "retrieval_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_contexts_chat_message_id_prompt_order",
                table: "chat_message_contexts",
                columns: new[] { "chat_message_id", "prompt_order" },
                unique: true,
                filter: "\"prompt_order\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_contexts_chunk_id",
                table: "chat_message_contexts",
                column: "chunk_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_message_contexts_prompt_order",
                table: "chat_message_contexts",
                sql: "(was_included_in_prompt = TRUE AND prompt_order >= 1) OR (was_included_in_prompt = FALSE AND prompt_order IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_message_contexts_retrieval_rank",
                table: "chat_message_contexts",
                sql: "retrieval_rank >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_request_messages_chat_message_id_message_order",
                table: "chat_message_request_messages",
                columns: new[] { "chat_message_id", "message_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_subject_snapshots_chat_message_id_subject_order",
                table: "chat_message_subject_snapshots",
                columns: new[] { "chat_message_id", "subject_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_subject_snapshots_subject_id",
                table: "chat_message_subject_snapshots",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_response_request_messages_test_response_id_message_ord",
                table: "test_response_request_messages",
                columns: new[] { "test_response_id", "message_order" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_message_contexts_chunks_chunk_id",
                table: "chat_message_contexts",
                column: "chunk_id",
                principalTable: "chunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_citations_chat_message_contexts_retrieval_snapshot_id",
                table: "citations",
                column: "retrieval_snapshot_id",
                principalTable: "chat_message_contexts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_citations_chunks_chunk_id",
                table: "citations",
                column: "chunk_id",
                principalTable: "chunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_test_response_contexts_chunks_chunk_id",
                table: "test_response_contexts",
                column: "chunk_id",
                principalTable: "chunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_test_responses_test_questions_test_question_id",
                table: "test_responses",
                column: "test_question_id",
                principalTable: "test_questions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "test_responses" WHERE "test_question_id" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot roll back reconstructible answer snapshots: test_responses contains rows without a live test_question_id.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM "citations" WHERE "chunk_id" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot roll back reconstructible answer snapshots: citations contains rows without a live chunk_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "chat_session_title_generation_metrics" WHERE "prompt_tokens" NOT BETWEEN -2147483648 AND 2147483647 OR "completion_tokens" NOT BETWEEN -2147483648 AND 2147483647
                        UNION ALL SELECT 1 FROM "chat_message_generation_metrics" WHERE "prompt_tokens" NOT BETWEEN -2147483648 AND 2147483647 OR "completion_tokens" NOT BETWEEN -2147483648 AND 2147483647
                        UNION ALL SELECT 1 FROM "test_responses" WHERE "prompt_tokens" NOT BETWEEN -2147483648 AND 2147483647 OR "completion_tokens" NOT BETWEEN -2147483648 AND 2147483647
                    ) THEN
                        RAISE EXCEPTION 'Cannot roll back reconstructible answer snapshots: token usage exceeds the 32-bit integer range required by the previous schema.';
                    END IF;
                END
                $migration$;

                DELETE FROM "chat_message_contexts" WHERE "was_included_in_prompt" = FALSE;
                UPDATE "chat_message_contexts" SET "retrieval_rank" = "prompt_order";

                DELETE FROM "test_response_contexts" WHERE "was_included_in_prompt" = FALSE;
                UPDATE "test_response_contexts" SET "retrieval_rank" = "prompt_order";
                """);

            migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_session_title_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "chat_message_generation_metrics", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "prompt_tokens", table: "test_responses", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "completion_tokens", table: "test_responses", type: "integer", nullable: true, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.DropColumn(name: "embedding_provider", table: "experiment_configuration_snapshots");
            migrationBuilder.DropColumn(name: "llm_provider", table: "experiment_configuration_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_message_contexts_chunks_chunk_id",
                table: "chat_message_contexts");

            migrationBuilder.DropForeignKey(
                name: "fk_citations_chat_message_contexts_retrieval_snapshot_id",
                table: "citations");

            migrationBuilder.DropForeignKey(
                name: "fk_citations_chunks_chunk_id",
                table: "citations");

            migrationBuilder.DropForeignKey(
                name: "fk_test_response_contexts_chunks_chunk_id",
                table: "test_response_contexts");

            migrationBuilder.DropForeignKey(
                name: "fk_test_responses_test_questions_test_question_id",
                table: "test_responses");

            migrationBuilder.DropTable(
                name: "chat_message_request_messages");

            migrationBuilder.DropTable(
                name: "chat_message_subject_snapshots");

            migrationBuilder.DropTable(
                name: "test_response_request_messages");

            migrationBuilder.DropIndex(
                name: "ix_test_response_contexts_chunk_id",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_test_response_contexts_test_response_id_prompt_order",
                table: "test_response_contexts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_response_contexts_prompt_order",
                table: "test_response_contexts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_response_contexts_retrieval_rank",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_citations_retrieval_snapshot_id",
                table: "citations");

            migrationBuilder.DropIndex(
                name: "ix_chat_message_contexts_chat_message_id_prompt_order",
                table: "chat_message_contexts");

            migrationBuilder.DropIndex(
                name: "ix_chat_message_contexts_chunk_id",
                table: "chat_message_contexts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_message_contexts_prompt_order",
                table: "chat_message_contexts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_message_contexts_retrieval_rank",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "dataset_key",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "dataset_name",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "dataset_version",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "ground_truth",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "question",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "question_difficulty",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "question_external_id",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "question_language",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "raw_generated_answer",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "reconstruction_completeness",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "source_question_id",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "chunk_id",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "chunk_index",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "document_file_name",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "document_title",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "end_page_number",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "end_section_title",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "prompt_order",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "similarity_score",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "source_chunk_id",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "source_subject_id",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "start_page_number",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "start_section_title",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "subject_code",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "subject_name",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "was_included_in_prompt",
                table: "test_response_contexts");

            migrationBuilder.DropColumn(
                name: "reconstruction_completeness",
                table: "experiments");

            migrationBuilder.DropColumn(
                name: "retrieval_snapshot_id",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "reconstruction_completeness",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "embedding_provider",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "llm_provider",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "reasoning_effort",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "reasoning_output",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "chunk_id",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "chunk_index",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "document_file_name",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "document_title",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "end_page_number",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "end_section_title",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "prompt_order",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "similarity_score",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "source_chunk_id",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "source_subject_id",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "start_page_number",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "start_section_title",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "subject_code",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "subject_name",
                table: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "was_included_in_prompt",
                table: "chat_message_contexts");

            migrationBuilder.RenameColumn(
                name: "retrieval_rank",
                table: "test_response_contexts",
                newName: "context_index");

            migrationBuilder.RenameColumn(
                name: "chunk_text",
                table: "test_response_contexts",
                newName: "context_text");

            migrationBuilder.RenameIndex(
                name: "ix_test_response_contexts_test_response_id_retrieval_rank",
                table: "test_response_contexts",
                newName: "ix_test_response_contexts_test_response_id_context_index");

            migrationBuilder.RenameColumn(
                name: "retrieval_rank",
                table: "chat_message_contexts",
                newName: "context_index");

            migrationBuilder.RenameColumn(
                name: "chunk_text",
                table: "chat_message_contexts",
                newName: "context_text");

            migrationBuilder.RenameIndex(
                name: "ix_chat_message_contexts_chat_message_id_retrieval_rank",
                table: "chat_message_contexts",
                newName: "ix_chat_message_contexts_chat_message_id_context_index");

            migrationBuilder.AlterColumn<int>(
                name: "test_question_id",
                table: "test_responses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "chunk_id",
                table: "citations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts",
                sql: "context_index >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts",
                sql: "context_index >= 1");

            migrationBuilder.AddForeignKey(
                name: "fk_citations_chunks_chunk_id",
                table: "citations",
                column: "chunk_id",
                principalTable: "chunks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_test_responses_test_questions_test_question_id",
                table: "test_responses",
                column: "test_question_id",
                principalTable: "test_questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
