using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixUpChatMessageConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE citations
                RENAME CONSTRAINT fk_citations_messages_message_id
                TO fk_citations_chat_messages_message_id;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "top_k",
                table: "chat_messages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE citations
                RENAME CONSTRAINT fk_citations_chat_messages_message_id
                TO fk_citations_messages_message_id;
                """);

            migrationBuilder.DropColumn(
                name: "top_k",
                table: "chat_messages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");
        }
    }
}
