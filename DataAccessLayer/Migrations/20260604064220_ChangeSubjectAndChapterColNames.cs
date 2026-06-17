using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSubjectAndChapterColNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "subject_name",
                table: "subjects",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "subject_code",
                table: "subjects",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "chapter_name",
                table: "chapters",
                newName: "name");

            //migrationBuilder.AlterColumn<int>(
            //    name: "file_type",
            //    table: "documents",
            //    type: "integer",
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "text");

            migrationBuilder.DropColumn(
                name: "file_type",
                table: "documents");

            migrationBuilder.AddColumn<int>(
                name: "file_type",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "subjects",
                newName: "subject_name");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "subjects",
                newName: "subject_code");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "chapters",
                newName: "chapter_name");

            migrationBuilder.AlterColumn<string>(
                name: "file_type",
                table: "documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
