using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePersistedOrdinalsToOneBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RequireExactSequence("parsed_sections", "document_id", "section_index", 0, "normalize"));
            migrationBuilder.Sql(RequireExactSequence("chunks", "document_id", "chunk_index", 0, "normalize"));
            migrationBuilder.Sql(RequireExactSequence("chat_message_contexts", "chat_message_id", "context_index", 0, "normalize"));
            migrationBuilder.Sql(RequireExactSequence("test_response_contexts", "test_response_id", "context_index", 0, "normalize"));
            migrationBuilder.Sql(RequireExactSequence("citation_occurrences", "citation_id", "occurrence_index", 0, "normalize"));

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_test_response_contexts_test_response_id_context_index",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_parsed_sections_document_id",
                table: "parsed_sections");

            migrationBuilder.DropIndex(
                name: "ix_citation_occurrences_citation_id",
                table: "citation_occurrences");

            migrationBuilder.DropIndex(
                name: "ix_chunks_document_id",
                table: "chunks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts");

            migrationBuilder.DropIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts");

            migrationBuilder.Sql("UPDATE parsed_sections SET section_index = section_index + 1;");
            migrationBuilder.Sql("UPDATE chunks SET chunk_index = chunk_index + 1;");
            migrationBuilder.Sql("UPDATE chat_message_contexts SET context_index = context_index + 1;");
            migrationBuilder.Sql("UPDATE test_response_contexts SET context_index = context_index + 1;");
            migrationBuilder.Sql("UPDATE citation_occurrences SET occurrence_index = occurrence_index + 1;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts",
                sql: "context_index >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_test_response_contexts_test_response_id_context_index",
                table: "test_response_contexts",
                columns: new[] { "test_response_id", "context_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parsed_sections_document_id_section_index",
                table: "parsed_sections",
                columns: new[] { "document_id", "section_index" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_parsed_sections_section_index",
                table: "parsed_sections",
                sql: "section_index >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_citation_occurrences_citation_id_occurrence_index",
                table: "citation_occurrences",
                columns: new[] { "citation_id", "occurrence_index" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_citation_occurrences_occurrence_index",
                table: "citation_occurrences",
                sql: "occurrence_index >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_chunks_document_id_chunk_index",
                table: "chunks",
                columns: new[] { "document_id", "chunk_index" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_chunks_chunk_index",
                table: "chunks",
                sql: "chunk_index >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts",
                sql: "context_index >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts",
                columns: new[] { "chat_message_id", "context_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RequireExactSequence("parsed_sections", "document_id", "section_index", 1, "roll back"));
            migrationBuilder.Sql(RequireExactSequence("chunks", "document_id", "chunk_index", 1, "roll back"));
            migrationBuilder.Sql(RequireExactSequence("chat_message_contexts", "chat_message_id", "context_index", 1, "roll back"));
            migrationBuilder.Sql(RequireExactSequence("test_response_contexts", "test_response_id", "context_index", 1, "roll back"));
            migrationBuilder.Sql(RequireExactSequence("citation_occurrences", "citation_id", "occurrence_index", 1, "roll back"));

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_test_response_contexts_test_response_id_context_index",
                table: "test_response_contexts");

            migrationBuilder.DropIndex(
                name: "ix_parsed_sections_document_id_section_index",
                table: "parsed_sections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_parsed_sections_section_index",
                table: "parsed_sections");

            migrationBuilder.DropIndex(
                name: "ix_citation_occurrences_citation_id_occurrence_index",
                table: "citation_occurrences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_citation_occurrences_occurrence_index",
                table: "citation_occurrences");

            migrationBuilder.DropIndex(
                name: "ix_chunks_document_id_chunk_index",
                table: "chunks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chunks_chunk_index",
                table: "chunks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts");

            migrationBuilder.DropIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts");

            migrationBuilder.Sql("UPDATE parsed_sections SET section_index = section_index - 1;");
            migrationBuilder.Sql("UPDATE chunks SET chunk_index = chunk_index - 1;");
            migrationBuilder.Sql("UPDATE chat_message_contexts SET context_index = context_index - 1;");
            migrationBuilder.Sql("UPDATE test_response_contexts SET context_index = context_index - 1;");
            migrationBuilder.Sql("UPDATE citation_occurrences SET occurrence_index = occurrence_index - 1;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_response_contexts_context_index",
                table: "test_response_contexts",
                sql: "context_index >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_parsed_sections_document_id",
                table: "parsed_sections",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_citation_occurrences_citation_id",
                table: "citation_occurrences",
                column: "citation_id");

            migrationBuilder.CreateIndex(
                name: "ix_chunks_document_id",
                table: "chunks",
                column: "document_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_message_contexts_context_index",
                table: "chat_message_contexts",
                sql: "context_index >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts",
                columns: new[] { "chat_message_id", "context_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_response_contexts_test_response_id_context_index",
                table: "test_response_contexts",
                columns: new[] { "test_response_id", "context_index" },
                unique: true);
        }

        private static string RequireExactSequence(string table, string parentColumn, string indexColumn, int firstIndex, string operation) =>
            $"""
            DO $$
            DECLARE invalid_parent text;
            BEGIN
                SELECT "{parentColumn}"::text INTO invalid_parent
                FROM "{table}"
                GROUP BY "{parentColumn}"
                HAVING MIN("{indexColumn}") <> {firstIndex}
                    OR MAX("{indexColumn}")::bigint <> COUNT(*) + {firstIndex - 1}
                    OR COUNT(DISTINCT "{indexColumn}") <> COUNT(*)
                LIMIT 1;

                IF invalid_parent IS NOT NULL THEN
                    RAISE EXCEPTION 'Cannot {operation} {table}: parent % does not have an exact contiguous sequence starting at {firstIndex}.', invalid_parent;
                END IF;
            END $$;
            """;
    }
}
