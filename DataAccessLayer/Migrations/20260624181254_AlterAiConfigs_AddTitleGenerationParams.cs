using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterAiConfigs_AddTitleGenerationParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "temperature",
                table: "subject_ai_configurations",
                newName: "chat_temperature");

            migrationBuilder.RenameColumn(
                name: "system_prompt",
                table: "subject_ai_configurations",
                newName: "chat_prompt");

            migrationBuilder.RenameColumn(
                name: "temperature",
                table: "global_ai_configurations",
                newName: "chat_temperature");

            migrationBuilder.RenameColumn(
                name: "system_prompt",
                table: "global_ai_configurations",
                newName: "chat_prompt");

            migrationBuilder.AddColumn<string>(
                name: "title_prompt",
                table: "subject_ai_configurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "title_temperature",
                table: "subject_ai_configurations",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_prompt",
                table: "global_ai_configurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "title_temperature",
                table: "global_ai_configurations",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "chat_sessions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_code",
                table: "subjects",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subjects_code",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "title_prompt",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "title_temperature",
                table: "subject_ai_configurations");

            migrationBuilder.DropColumn(
                name: "title_prompt",
                table: "global_ai_configurations");

            migrationBuilder.DropColumn(
                name: "title_temperature",
                table: "global_ai_configurations");

            migrationBuilder.RenameColumn(
                name: "chat_temperature",
                table: "subject_ai_configurations",
                newName: "temperature");

            migrationBuilder.RenameColumn(
                name: "chat_prompt",
                table: "subject_ai_configurations",
                newName: "system_prompt");

            migrationBuilder.RenameColumn(
                name: "chat_temperature",
                table: "global_ai_configurations",
                newName: "temperature");

            migrationBuilder.RenameColumn(
                name: "chat_prompt",
                table: "global_ai_configurations",
                newName: "system_prompt");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "chat_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
