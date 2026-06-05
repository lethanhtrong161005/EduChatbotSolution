using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixParsedSectionNotHavingTimestampTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "parsed_sections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER ""UpdateTimestamp""
                        BEFORE UPDATE ON ""parsed_sections""
                        FOR EACH ROW
                        EXECUTE FUNCTION ""Update_Timestamp_Function""();
                "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "parsed_sections",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.Sql(
                @"
                    DROP TRIGGER ""UpdateTimestamp"" ON ""parsed_sections"";
                "
            );
        }
    }
}
