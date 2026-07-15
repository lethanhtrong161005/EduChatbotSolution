using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chat_messages_chat_session_id",
                table: "chat_messages");

            migrationBuilder.AddColumn<Guid>(
                name: "in_reply_to_message_id",
                table: "chat_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_selected_variant",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "message_index",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "variant_index",
                table: "chat_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        WITH ordered AS (
                            SELECT
                                chat_session_id,
                                chat_role,
                                row_number() OVER (
                                    PARTITION BY chat_session_id
                                    ORDER BY sent_at, created_at, id) AS row_number,
                                count(*) OVER (PARTITION BY chat_session_id) AS message_count
                            FROM chat_messages
                        )
                        SELECT 1
                        FROM ordered
                        WHERE chat_role NOT IN (1, 2)
                           OR message_count % 2 <> 0
                           OR (row_number % 2 = 1 AND chat_role <> 1)
                           OR (row_number % 2 = 0 AND chat_role <> 2)
                    ) THEN
                        RAISE EXCEPTION 'Chat variant backfill aborted: a session is not a strict user/assistant sequence.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                WITH ordered AS (
                    SELECT
                        id,
                        chat_role,
                        row_number() OVER (
                            PARTITION BY chat_session_id
                            ORDER BY sent_at, created_at, id) AS message_index,
                        lag(id) OVER (
                            PARTITION BY chat_session_id
                            ORDER BY sent_at, created_at, id) AS preceding_message_id
                    FROM chat_messages
                )
                UPDATE chat_messages AS message
                SET
                    message_index = ordered.message_index,
                    in_reply_to_message_id = CASE WHEN ordered.chat_role = 2 THEN ordered.preceding_message_id END,
                    variant_index = CASE WHEN ordered.chat_role = 2 THEN 1 END,
                    is_selected_variant = ordered.chat_role = 2
                FROM ordered
                WHERE message.id = ordered.id;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_chat_messages_id_chat_session_id",
                table: "chat_messages",
                columns: new[] { "id", "chat_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_session_id_message_index",
                table: "chat_messages",
                columns: new[] { "chat_session_id", "message_index" },
                unique: true,
                filter: "\"chat_role\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_session_id_message_index_variant_index",
                table: "chat_messages",
                columns: new[] { "chat_session_id", "message_index", "variant_index" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_in_reply_to_message_id",
                table: "chat_messages",
                column: "in_reply_to_message_id",
                unique: true,
                filter: "\"is_selected_variant\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_in_reply_to_message_id_chat_session_id",
                table: "chat_messages",
                columns: new[] { "in_reply_to_message_id", "chat_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_in_reply_to_message_id_variant_index",
                table: "chat_messages",
                columns: new[] { "in_reply_to_message_id", "variant_index" },
                unique: true,
                filter: "\"chat_role\" = 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chat_messages_turn_variant_shape",
                table: "chat_messages",
                sql: "(chat_role = 0 AND message_index IS NULL AND in_reply_to_message_id IS NULL AND variant_index IS NULL AND is_selected_variant = FALSE) OR (chat_role = 1 AND message_index > 0 AND message_index % 2 = 1 AND in_reply_to_message_id IS NULL AND variant_index IS NULL AND is_selected_variant = FALSE) OR (chat_role = 2 AND message_index > 0 AND message_index % 2 = 0 AND in_reply_to_message_id IS NOT NULL AND variant_index > 0)");

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_chat_messages_in_reply_to_message_id_chat_ses",
                table: "chat_messages",
                columns: new[] { "in_reply_to_message_id", "chat_session_id" },
                principalTable: "chat_messages",
                principalColumns: new[] { "id", "chat_session_id" });

            migrationBuilder.Sql(
                """
                CREATE FUNCTION validate_chat_message_variants()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM chat_messages AS assistant
                        LEFT JOIN chat_messages AS user_message
                          ON user_message.id = assistant.in_reply_to_message_id
                         AND user_message.chat_session_id = assistant.chat_session_id
                        WHERE assistant.chat_role = 2
                          AND (
                              user_message.id IS NULL
                              OR user_message.chat_role <> 1
                              OR assistant.message_index <> user_message.message_index + 1)
                    ) THEN
                        RAISE EXCEPTION 'Assistant variants must reply to the preceding user slot in the same session.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM chat_messages AS assistant
                        WHERE assistant.chat_role = 2
                        GROUP BY assistant.in_reply_to_message_id, assistant.chat_session_id
                        HAVING count(*) FILTER (WHERE assistant.is_selected_variant) <> 1
                    ) THEN
                        RAISE EXCEPTION 'Each user turn with assistant variants must have exactly one selected variant.';
                    END IF;

                    RETURN NULL;
                END;
                $$;

                CREATE CONSTRAINT TRIGGER "ValidateChatMessageVariants"
                    AFTER INSERT OR UPDATE OR DELETE ON chat_messages
                    DEFERRABLE INITIALLY DEFERRED
                    FOR EACH ROW
                    EXECUTE FUNCTION validate_chat_message_variants();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "ValidateChatMessageVariants" ON chat_messages;
                DROP FUNCTION IF EXISTS validate_chat_message_variants();
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_chat_messages_in_reply_to_message_id_chat_ses",
                table: "chat_messages");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_chat_messages_id_chat_session_id",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_chat_session_id_message_index",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_chat_session_id_message_index_variant_index",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_in_reply_to_message_id",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_in_reply_to_message_id_chat_session_id",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_in_reply_to_message_id_variant_index",
                table: "chat_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chat_messages_turn_variant_shape",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "in_reply_to_message_id",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_selected_variant",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "message_index",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "variant_index",
                table: "chat_messages");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_session_id",
                table: "chat_messages",
                column: "chat_session_id");
        }
    }
}
