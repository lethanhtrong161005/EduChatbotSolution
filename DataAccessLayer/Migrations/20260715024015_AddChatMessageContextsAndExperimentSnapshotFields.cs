using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageContextsAndExperimentSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "citation_extraction_prompt",
                table: "experiment_configuration_snapshots",
                type: "text",
                nullable: false);

            migrationBuilder.AddColumn<float>(
                name: "citation_extraction_temperature",
                table: "experiment_configuration_snapshots",
                type: "real",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "chat_message_contexts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context_index = table.Column<int>(type: "integer", nullable: false),
                    context_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_message_contexts", x => x.id);
                    table.CheckConstraint("ck_chat_message_contexts_context_index", "context_index >= 0");
                    table.ForeignKey(
                        name: "fk_chat_message_contexts_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_message_contexts_chat_message_id_context_index",
                table: "chat_message_contexts",
                columns: new[] { "chat_message_id", "context_index" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_message_contexts"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message_contexts");

            migrationBuilder.DropColumn(
                name: "citation_extraction_prompt",
                table: "experiment_configuration_snapshots");

            migrationBuilder.DropColumn(
                name: "citation_extraction_temperature",
                table: "experiment_configuration_snapshots");
        }
    }
}
