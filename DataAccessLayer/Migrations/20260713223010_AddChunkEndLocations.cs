using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkEndLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "section_title",
                table: "chunks",
                newName: "start_section_title");

            migrationBuilder.RenameColumn(
                name: "page_number",
                table: "chunks",
                newName: "start_page_number");

            migrationBuilder.AddColumn<int>(
                name: "end_page_number",
                table: "chunks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_section_title",
                table: "chunks",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_chunks_end_page_number",
                table: "chunks",
                sql: "end_page_number >= start_page_number");

            migrationBuilder.Sql(
                """
                UPDATE chunks
                SET
                    end_page_number = start_page_number,
                    end_section_title = start_section_title
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_chunks_end_page_number",
                table: "chunks");

            migrationBuilder.DropColumn(
                name: "end_page_number",
                table: "chunks");

            migrationBuilder.DropColumn(
                name: "end_section_title",
                table: "chunks");

            migrationBuilder.RenameColumn(
                name: "start_section_title",
                table: "chunks",
                newName: "section_title");

            migrationBuilder.RenameColumn(
                name: "start_page_number",
                table: "chunks",
                newName: "page_number");
        }
    }
}
