using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChatGenerationSettingsAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chat_sessions_subjects_subject_id",
                table: "chat_sessions");

            migrationBuilder.RenameColumn(
                name: "retrieval_top_k",
                table: "subject_ai_configurations",
                newName: "top_k");

            migrationBuilder.RenameColumn(
                name: "response_time_ms",
                table: "chat_messages",
                newName: "total_response_time_ms");

            migrationBuilder.RenameColumn(
                name: "generation_temperature",
                table: "chat_messages",
                newName: "temperature");

            migrationBuilder.AddColumn<int>(
                name: "max_context_chunks",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_history_messages",
                table: "subject_ai_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "system_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "temperature",
                table: "subject_ai_configurations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "citation_index",
                table: "citations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "location_in_document",
                table: "citations",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "subject_id",
                table: "chat_sessions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<long>(
                name: "retrieval_time_ms",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "tokens_per_second",
                table: "chat_messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "time_to_first_token_ms",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_sessions_subjects_subject_id",
                table: "chat_sessions",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chat_sessions_subjects_subject_id",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "max_context_chunks",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "max_history_messages",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "system_prompt",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "temperature",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "citation_index",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "location_in_document",
                table: "citations");

            migrationBuilder.DropColumn(
                name: "retrieval_time_ms",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "tokens_per_second",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "time_to_first_token_ms",
                table: "chat_messages");

            migrationBuilder.RenameColumn(
                name: "top_k",
                table: "subject_ai_configurations",
                newName: "retrieval_top_k");

            migrationBuilder.RenameColumn(
                name: "total_response_time_ms",
                table: "chat_messages",
                newName: "response_time_ms");

            migrationBuilder.RenameColumn(
                name: "temperature",
                table: "chat_messages",
                newName: "generation_temperature");

            migrationBuilder.AlterColumn<int>(
                name: "subject_id",
                table: "chat_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_sessions_subjects_subject_id",
                table: "chat_sessions",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
