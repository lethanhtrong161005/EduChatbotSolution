using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestampGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscription_plan_options_subscription_plan_id",
                table: "subscription_plan_options");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "user_subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "test_responses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "test_questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_purchases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_plans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_plan_options",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subjects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "payment_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "experiments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "citations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chunks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chapters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plan_options_subscription_plan_id_duration_days",
                table: "subscription_plan_options",
                columns: new[] { "subscription_plan_id", "duration_days" },
                unique: true);

            migrationBuilder.Sql(
@"
    CREATE FUNCTION ""Update_Timestamp_Function""()
    RETURNS TRIGGER
    LANGUAGE PLPGSQL
    AS $$
    BEGIN
        NEW.""updated_at"" := now();
        RETURN NEW;
    END;
    $$;

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""subscription_plans""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""subscription_plan_options""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""subscription_purchases""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""user_subscriptions""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""payment_transactions""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""subjects""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""chapters""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""documents""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""chunks""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""conversations""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""messages""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""citations""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""test_questions""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""experiments""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();

    CREATE TRIGGER ""UpdateTimestamp""
        BEFORE UPDATE ON ""test_responses""
        FOR EACH ROW
        EXECUTE FUNCTION ""Update_Timestamp_Function""();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscription_plan_options_subscription_plan_id_duration_days",
                table: "subscription_plan_options");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "user_subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "test_responses",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "test_questions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_purchases",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_plans",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subscription_plan_options",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "subjects",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "payment_transactions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "experiments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "citations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chunks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "chapters",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plan_options_subscription_plan_id",
                table: "subscription_plan_options",
                column: "subscription_plan_id");

            migrationBuilder.Sql(
@"
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""subscription_plans"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""subscription_plan_options"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""subscription_purchases"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""user_subscriptions"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""payment_transactions"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""subjects"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""chapters"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""documents"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""chunks"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""conversations"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""messages"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""citations"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""test_questions"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""experiments"";
    DROP TRIGGER IF EXISTS ""UpdateTimestamp"" ON ""test_responses"";

    DROP FUNCTION IF EXISTS ""Update_Timestamp_Function""();
");
        }
    }
}
