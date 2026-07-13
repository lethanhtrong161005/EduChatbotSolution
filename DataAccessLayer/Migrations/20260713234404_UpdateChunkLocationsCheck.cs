using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChunkLocationsCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_chunks_end_page_number",
                table: "chunks");

            migrationBuilder.Sql(
                """
                UPDATE chunks SET end_page_number = start_page_number
                WHERE start_page_number IS NOT NULL AND end_page_number IS NULL;

                UPDATE chunks SET start_page_number = end_page_number
                WHERE start_page_number IS NULL AND end_page_number IS NOT NULL;

                UPDATE chunks SET end_section_title = start_section_title
                WHERE start_section_title IS NOT NULL AND end_section_title IS NULL;

                UPDATE chunks SET start_section_title = end_section_title
                WHERE start_section_title IS NULL AND end_section_title IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_chunks_page_numbers",
                table: "chunks",
                sql: "(start_page_number IS NULL AND end_page_number IS NULL) OR (start_page_number IS NOT NULL AND end_page_number IS NOT NULL AND end_page_number >= start_page_number)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chunks_section_titles",
                table: "chunks",
                sql: "(start_section_title IS NULL AND end_section_title IS NULL) OR (start_section_title IS NOT NULL AND end_section_title IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_chunks_page_numbers",
                table: "chunks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chunks_section_titles",
                table: "chunks");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chunks_end_page_number",
                table: "chunks",
                sql: "end_page_number >= start_page_number");
        }
    }
}
