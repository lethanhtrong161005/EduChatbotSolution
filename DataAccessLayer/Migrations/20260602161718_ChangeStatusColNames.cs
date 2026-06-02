using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStatusColNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "payments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "order_status",
                table: "orders",
                newName: "status");

            migrationBuilder.AlterColumn<string>(
                name: "external_transaction_code",
                table: "payments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "payments",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "orders",
                newName: "order_status");

            migrationBuilder.AlterColumn<string>(
                name: "external_transaction_code",
                table: "payments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
