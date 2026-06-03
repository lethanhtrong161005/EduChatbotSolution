using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIndexingEntitiesAndProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "is_indexed",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "sender_role",
                table: "messages",
                newName: "chat_role");

            migrationBuilder.RenameColumn(
                name: "conversation_id",
                table: "messages",
                newName: "chat_session_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_conversation_id",
                table: "messages",
                newName: "ix_messages_chat_session_id");

            migrationBuilder.RenameColumn(
                name: "vector_id",
                table: "chunks",
                newName: "section_title");

            migrationBuilder.AddColumn<double>(
                name: "generation_temperature",
                table: "messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "llm_model",
                table: "messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "response_time_ms",
                table: "messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retrieved_chunk_count",
                table: "messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "extracted_text",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "indexing_errors",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parser_used",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "subject_id",
                table: "conversations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "similarity_score",
                table: "citations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Vector>(
                name: "embedding",
                table: "chunks",
                type: "vector(1536)",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "page_number",
                table: "chunks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_comments_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subject_ai_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunking_strategy = table.Column<string>(type: "text", nullable: false),
                    embedding_model = table.Column<string>(type: "text", nullable: false),
                    llm_model = table.Column<string>(type: "text", nullable: false),
                    retrieval_top_k = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_ai_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_subject_ai_configurations_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subject_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_subject_memberships_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subject_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_subject_id",
                table: "conversations",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_comments_document_id",
                table: "document_comments",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_comments_user_id",
                table: "document_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_ai_configurations_subject_id",
                table: "subject_ai_configurations",
                column: "subject_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_subject_id_role",
                table: "subject_memberships",
                columns: new[] { "subject_id", "role" },
                unique: true,
                filter: "\"role\" = 2");

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_user_id_subject_id",
                table: "subject_memberships",
                columns: new[] { "user_id", "subject_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_subjects_subject_id",
                table: "conversations",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_conversations_chat_session_id",
                table: "messages",
                column: "chat_session_id",
                principalTable: "conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER ""UpdateTimestamp""
                        BEFORE UPDATE ON ""subject_memberships""
                        FOR EACH ROW
                        EXECUTE FUNCTION ""Update_Timestamp_Function""();

                    CREATE TRIGGER ""UpdateTimestamp""
                        BEFORE UPDATE ON ""subject_ai_configurations""
                        FOR EACH ROW
                        EXECUTE FUNCTION ""Update_Timestamp_Function""();

                    CREATE TRIGGER ""UpdateTimestamp""
                        BEFORE UPDATE ON ""document_comments""
                        FOR EACH ROW
                        EXECUTE FUNCTION ""Update_Timestamp_Function""();
                "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversations_subjects_subject_id",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_conversations_chat_session_id",
                table: "messages");

            migrationBuilder.DropTable(
                name: "document_comments");

            migrationBuilder.DropTable(
                name: "subject_ai_configurations");

            migrationBuilder.DropTable(
                name: "subject_memberships");

            migrationBuilder.DropIndex(
                name: "ix_conversations_subject_id",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "generation_temperature",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "llm_model",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "response_time_ms",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "retrieved_chunk_count",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "description",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "extracted_text",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "indexing_errors",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "parser_used",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "status",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "type",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "similarity_score",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "embedding",
                table: "chunks");

            migrationBuilder.DropColumn(
                name: "page_number",
                table: "chunks");

            migrationBuilder.RenameColumn(
                name: "chat_session_id",
                table: "messages",
                newName: "conversation_id");

            migrationBuilder.RenameColumn(
                name: "chat_role",
                table: "messages",
                newName: "sender_role");

            migrationBuilder.RenameIndex(
                name: "ix_messages_chat_session_id",
                table: "messages",
                newName: "ix_messages_conversation_id");

            migrationBuilder.RenameColumn(
                name: "section_title",
                table: "chunks",
                newName: "vector_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_indexed",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                @"
                    DROP TRIGGER ""UpdateTimestamp"" ON ""subject_memberships"";
                    DROP TRIGGER ""UpdateTimestamp"" ON ""subject_ai_configurations"";
                    DROP TRIGGER ""UpdateTimestamp"" ON ""document_comments"";
                "
            );
        }
    }
}
