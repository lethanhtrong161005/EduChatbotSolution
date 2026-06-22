using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterAiConfigsAndChatMessage_AddPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "context_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "no_context_retrieved_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "context_prompt",
                table: "global_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "no_context_retrieved_prompt",
                table: "global_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "context_prompt",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "no_context_retrieved_prompt",
                table: "chat_message_generation_settings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "context_prompt",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "no_context_retrieved_prompt",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "context_prompt",
                table: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "no_context_retrieved_prompt",
                table: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "context_prompt",
                table: "chat_message_generation_settings");

            migrationBuilder.DropColumn(
                name: "no_context_retrieved_prompt",
                table: "chat_message_generation_settings");
        }
    }
}
