using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterChapter_AddUniqueIndex_SubjectId_ChapterNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chapters_subject_id",
                table: "chapters");

            migrationBuilder.CreateIndex(
                name: "ix_chapters_subject_id_chapter_number",
                table: "chapters",
                columns: new[] { "subject_id", "chapter_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chapters_subject_id_chapter_number",
                table: "chapters");

            migrationBuilder.CreateIndex(
                name: "ix_chapters_subject_id",
                table: "chapters",
                column: "subject_id");
        }
    }
}
