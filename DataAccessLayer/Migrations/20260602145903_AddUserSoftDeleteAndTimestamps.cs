using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSoftDeleteAndTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "daily_chat_limit",
                table: "subscription_plans",
                newName: "file_library_limit");

            migrationBuilder.RenameColumn(
                name: "allow_file_upload",
                table: "subscription_plans",
                newName: "is_featured");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

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
                name: "daily_message_quota",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "chat_session_limit",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "daily_file_upload_quota",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "daily_message_quota",
                table: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "subscription_plans",
                newName: "allow_file_upload");

            migrationBuilder.RenameColumn(
                name: "file_library_limit",
                table: "subscription_plans",
                newName: "daily_chat_limit");
        }
    }
}
