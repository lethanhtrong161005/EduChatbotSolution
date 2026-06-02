using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ChangePlanFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "daily_chat_limit",
                table: "subscription_plans",
                newName: "daily_message_quota");

            migrationBuilder.RenameColumn(
                name: "allow_file_upload",
                table: "subscription_plans",
                newName: "is_featured");

            migrationBuilder.AddColumn<int>(
                name: "chat_session_limit",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "daily_file_upload_quota",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "file_library_limit",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "external_transaction_code",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat_session_limit",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "daily_file_upload_quota",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "file_library_limit",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "external_transaction_code",
                table: "payment_transactions");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "subscription_plans",
                newName: "allow_file_upload");

            migrationBuilder.RenameColumn(
                name: "daily_message_quota",
                table: "subscription_plans",
                newName: "daily_chat_limit");
        }
    }
}
