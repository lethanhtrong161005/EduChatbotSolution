using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTbls_ChatTitleGenParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_session_title_generation_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    response_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_session_title_generation_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_session_title_generation_metrics_chat_sessions_id",
                        column: x => x.id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_session_title_generation_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    llm_model = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<float>(type: "real", nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_session_title_generation_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_session_title_generation_settings_chat_sessions_id",
                        column: x => x.id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_session_title_generation_settings"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();

                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "chat_session_title_generation_metrics"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_session_title_generation_metrics");

            migrationBuilder.DropTable(
                name: "chat_session_title_generation_settings");
        }
    }
}
