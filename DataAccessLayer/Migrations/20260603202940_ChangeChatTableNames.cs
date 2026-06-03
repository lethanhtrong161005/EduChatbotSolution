using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeChatTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "conversations",
                newName: "chat_sessions");

            migrationBuilder.RenameTable(
                name: "messages",
                newName: "chat_messages");

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT pk_conversations
                TO pk_chat_sessions;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_messages
                RENAME CONSTRAINT pk_messages
                TO pk_chat_messages;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT fk_conversations_users_user_id
                TO fk_chat_sessions_users_user_id;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT fk_conversations_subjects_subject_id
                TO fk_chat_sessions_subjects_subject_id;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_messages
                RENAME CONSTRAINT fk_messages_conversations_chat_session_id
                TO fk_chat_messages_chat_sessions_chat_session_id;
                """);

            migrationBuilder.RenameIndex(
                name: "ix_messages_chat_session_id",
                table: "chat_messages",
                newName: "ix_chat_messages_chat_session_id");

            migrationBuilder.RenameIndex(
                name: "ix_conversations_user_id",
                table: "chat_sessions",
                newName: "ix_chat_sessions_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_conversations_subject_id",
                table: "chat_sessions",
                newName: "ix_chat_sessions_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_chunks_embedding",
                table: "chunks",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 128)
                .Annotation("Npgsql:StorageParameter:m", 32);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "chat_sessions",
                newName: "conversations");

            migrationBuilder.RenameTable(
                name: "chat_messages",
                newName: "messages");

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT pk_conversations
                TO pk_chat_sessions;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_messages
                RENAME CONSTRAINT pk_messages
                TO pk_chat_messages;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT fk_chat_sessions_users_user_id
                TO fk_conversations_users_user_id;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_sessions
                RENAME CONSTRAINT fk_chat_sessions_subjects_subject_id
                TO fk_conversations_subjects_subject_id;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE chat_messages
                RENAME CONSTRAINT fk_chat_messages_chat_sessions_chat_session_id
                TO fk_messages_conversations_chat_session_id;
                """);

            migrationBuilder.RenameIndex(
                name: "ix_chat_sessions_user_id",
                table: "conversations",
                newName: "ix_conversations_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_chat_sessions_subject_id",
                table: "conversations",
                newName: "ix_conversations_subject_id");

            migrationBuilder.RenameIndex(
                name: "ix_chat_messages_chat_session_id",
                table: "messages",
                newName: "ix_messages_chat_session_id");

            migrationBuilder.DropIndex(
                name: "ix_chunks_embedding",
                table: "chunks");
        }
    }
}
