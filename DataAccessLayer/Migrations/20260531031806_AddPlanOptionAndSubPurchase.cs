using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanOptionAndSubPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_user_subscriptions_subscription_id",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_user_subscriptions_subscription_plans_subscription_plan_id",
                table: "user_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_user_subscriptions_subscription_plan_id",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "subscription_plan_id",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "price",
                table: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "payment_transactions",
                newName: "subscription_purchase_id");

            migrationBuilder.RenameIndex(
                name: "ix_payment_transactions_subscription_id",
                table: "payment_transactions",
                newName: "ix_payment_transactions_subscription_purchase_id");

            // ###############################

            //migrationBuilder.AlterColumn<int>(
            //    name: "payment_method",
            //    table: "payment_transactions",
            //    type: "integer",
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "text");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "payment_transactions");

            migrationBuilder.AddColumn<int>(
                name: "payment_method",
                table: "payment_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ###############################

            migrationBuilder.CreateTable(
                name: "subscription_plan_option",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscription_plan_id = table.Column<int>(type: "integer", nullable: false),
                    option_name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "money", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plan_option", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_plan_option_subscription_plans_subscription_pl",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_purchase",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_option_id = table.Column<int>(type: "integer", nullable: false),
                    user_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charged_amount = table.Column<decimal>(type: "money", nullable: false),
                    purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_purchase", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_purchase_subscription_plan_option_subscription",
                        column: x => x.subscription_plan_option_id,
                        principalTable: "subscription_plan_option",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subscription_purchase_user_subscriptions_user_subscription_",
                        column: x => x.user_subscription_id,
                        principalTable: "user_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_tier",
                table: "subscription_plans",
                column: "tier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plan_option_subscription_plan_id",
                table: "subscription_plan_option",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_purchase_subscription_plan_option_id",
                table: "subscription_purchase",
                column: "subscription_plan_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_purchase_user_subscription_id",
                table: "subscription_purchase",
                column: "user_subscription_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_subscription_purchase_subscription_pur",
                table: "payment_transactions",
                column: "subscription_purchase_id",
                principalTable: "subscription_purchase",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_subscription_purchase_subscription_pur",
                table: "payment_transactions");

            migrationBuilder.DropTable(
                name: "subscription_purchase");

            migrationBuilder.DropTable(
                name: "subscription_plan_option");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_tier",
                table: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "subscription_purchase_id",
                table: "payment_transactions",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "ix_payment_transactions_subscription_purchase_id",
                table: "payment_transactions",
                newName: "ix_payment_transactions_subscription_id");

            migrationBuilder.AddColumn<int>(
                name: "subscription_plan_id",
                table: "user_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "subscription_plans",
                type: "money",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "payment_method",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "ix_user_subscriptions_subscription_plan_id",
                table: "user_subscriptions",
                column: "subscription_plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_user_subscriptions_subscription_id",
                table: "payment_transactions",
                column: "subscription_id",
                principalTable: "user_subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_subscriptions_subscription_plans_subscription_plan_id",
                table: "user_subscriptions",
                column: "subscription_plan_id",
                principalTable: "subscription_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
