using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class TouchUpNamesAndNullabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_subscription_purchase_subscription_pur",
                table: "payment_transactions");

            migrationBuilder.DropTable(
                name: "subscription_purchase");

            migrationBuilder.DropTable(
                name: "subscription_plan_option");

            migrationBuilder.DropColumn(
                name: "ragas_context_precision",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "ragas_context_recall",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "ragas_faithfulness",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "ragas_response_relevancy",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "completion_token_count",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "prompt_token_count",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "size",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "subjects",
                newName: "subject_name");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "payment_transactions",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "experiments",
                newName: "experiment_name");

            migrationBuilder.RenameColumn(
                name: "path",
                table: "documents",
                newName: "file_path");

            migrationBuilder.RenameColumn(
                name: "text",
                table: "chunks",
                newName: "chunk_text");

            migrationBuilder.RenameColumn(
                name: "chunk_idex",
                table: "chunks",
                newName: "chunk_index");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "chapters",
                newName: "chapter_name");

            migrationBuilder.AddColumn<double>(
                name: "answer_relevancy",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_precision",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "context_recall",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "faithfulness",
                table: "test_responses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "difficulty",
                table: "test_questions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "subscription_plans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_method",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "paid_at",
                table: "payment_transactions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "completion_tokens",
                table: "messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prompt_tokens",
                table: "messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_at",
                table: "messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "experiments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<double>(
                name: "average_ragas_score",
                table: "experiments",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size",
                table: "documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_indexed",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "conversations",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "vector_id",
                table: "chunks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "token_count",
                table: "chunks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "chapter_number",
                table: "chapters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "subscription_plan_options",
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
                    table.PrimaryKey("pk_subscription_plan_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_plan_options_subscription_plans_subscription_p",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_purchases",
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
                    table.PrimaryKey("pk_subscription_purchases", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_purchases_subscription_plan_options_subscripti",
                        column: x => x.subscription_plan_option_id,
                        principalTable: "subscription_plan_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subscription_purchases_user_subscriptions_user_subscription",
                        column: x => x.user_subscription_id,
                        principalTable: "user_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plan_options_subscription_plan_id",
                table: "subscription_plan_options",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_purchases_subscription_plan_option_id",
                table: "subscription_purchases",
                column: "subscription_plan_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_purchases_user_subscription_id",
                table: "subscription_purchases",
                column: "user_subscription_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_subscription_purchases_subscription_pu",
                table: "payment_transactions",
                column: "subscription_purchase_id",
                principalTable: "subscription_purchases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_subscription_purchases_subscription_pu",
                table: "payment_transactions");

            migrationBuilder.DropTable(
                name: "subscription_purchases");

            migrationBuilder.DropTable(
                name: "subscription_plan_options");

            migrationBuilder.DropColumn(
                name: "answer_relevancy",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "context_precision",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "context_recall",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "faithfulness",
                table: "test_responses");

            migrationBuilder.DropColumn(
                name: "description",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "completion_tokens",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "prompt_tokens",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "original_file_name",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "file_size",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "is_indexed",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "uploaded_at",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "title",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "chapter_number",
                table: "chapters");

            migrationBuilder.RenameColumn(
                name: "subject_name",
                table: "subjects",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "payment_transactions",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "experiment_name",
                table: "experiments",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "file_path",
                table: "documents",
                newName: "path");

            migrationBuilder.RenameColumn(
                name: "chunk_text",
                table: "chunks",
                newName: "text");

            migrationBuilder.RenameColumn(
                name: "chunk_index",
                table: "chunks",
                newName: "chunk_idex");

            migrationBuilder.RenameColumn(
                name: "chapter_name",
                table: "chapters",
                newName: "name");

            migrationBuilder.AddColumn<double>(
                name: "ragas_context_precision",
                table: "test_responses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ragas_context_recall",
                table: "test_responses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ragas_faithfulness",
                table: "test_responses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ragas_response_relevancy",
                table: "test_responses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "difficulty",
                table: "test_questions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "subscription_plans",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "payment_method",
                table: "payment_transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "paid_at",
                table: "payment_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "completion_token_count",
                table: "messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "prompt_token_count",
                table: "messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "experiments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "average_ragas_score",
                table: "experiments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "size",
                table: "documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "vector_id",
                table: "chunks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "token_count",
                table: "chunks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "subscription_plan_option",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscription_plan_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    option_name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "money", nullable: false),
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
    }
}
