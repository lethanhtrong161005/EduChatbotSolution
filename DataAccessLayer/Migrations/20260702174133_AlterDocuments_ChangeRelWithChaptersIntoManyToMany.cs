using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AlterDocuments_ChangeRelWithChaptersIntoManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_comments_users_user_id",
                table: "document_comments");

            migrationBuilder.DropForeignKey(
                name: "fk_documents_chapters_chapter_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_subject_memberships_user_id_subject_id",
                table: "subject_memberships");

            migrationBuilder.RenameColumn(
                name: "chapter_id",
                table: "documents",
                newName: "subject_id");

            migrationBuilder.RenameIndex(
                name: "ix_documents_chapter_id",
                table: "documents",
                newName: "ix_documents_subject_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "document_comments",
                newName: "author_id");

            migrationBuilder.RenameIndex(
                name: "ix_document_comments_user_id",
                table: "document_comments",
                newName: "ix_document_comments_author_id");

            migrationBuilder.AlterColumn<int>(
                name: "chapter_number",
                table: "chapters",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_documents_id_subject_id",
                table: "documents",
                columns: new[] { "id", "subject_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_chapters_id_subject_id",
                table: "chapters",
                columns: new[] { "id", "subject_id" });

            migrationBuilder.CreateTable(
                name: "document_chapters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_id = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_chapters", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_chapters_chapters_chapter_id_subject_id",
                        columns: x => new { x.chapter_id, x.subject_id },
                        principalTable: "chapters",
                        principalColumns: new[] { "id", "subject_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_chapters_documents_document_id_subject_id",
                        columns: x => new { x.document_id, x.subject_id },
                        principalTable: "documents",
                        principalColumns: new[] { "id", "subject_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_subject_id_user_id",
                table: "subject_memberships",
                columns: new[] { "subject_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_subject_id",
                table: "subject_memberships",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_user_id",
                table: "subject_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_chapters_chapter_id_subject_id",
                table: "document_chapters",
                columns: new[] { "chapter_id", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "ix_document_chapters_document_id_chapter_id",
                table: "document_chapters",
                columns: new[] { "document_id", "chapter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_chapters_document_id_subject_id",
                table: "document_chapters",
                columns: new[] { "document_id", "subject_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_document_comments_users_author_id",
                table: "document_comments",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_subjects_subject_id",
                table: "documents",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                CREATE TRIGGER "update_timestamp"
                    BEFORE UPDATE ON "document_chapters"
                    FOR EACH ROW
                    EXECUTE FUNCTION "update_timestamp"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_comments_users_author_id",
                table: "document_comments");

            migrationBuilder.DropForeignKey(
                name: "fk_documents_subjects_subject_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "document_chapters");

            migrationBuilder.DropIndex(
                name: "ix_subject_memberships_subject_id_user_id",
                table: "subject_memberships");

            migrationBuilder.DropIndex(
                name: "ix_subject_memberships_subject_id",
                table: "subject_memberships");

            migrationBuilder.DropIndex(
                name: "ix_subject_memberships_user_id",
                table: "subject_memberships");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_documents_id_subject_id",
                table: "documents");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_chapters_id_subject_id",
                table: "chapters");

            migrationBuilder.RenameColumn(
                name: "subject_id",
                table: "documents",
                newName: "chapter_id");

            migrationBuilder.RenameIndex(
                name: "ix_documents_subject_id",
                table: "documents",
                newName: "ix_documents_chapter_id");

            migrationBuilder.RenameColumn(
                name: "author_id",
                table: "document_comments",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_document_comments_author_id",
                table: "document_comments",
                newName: "ix_document_comments_user_id");

            migrationBuilder.AlterColumn<int>(
                name: "chapter_number",
                table: "chapters",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "ix_subject_memberships_user_id_subject_id",
                table: "subject_memberships",
                columns: new[] { "user_id", "subject_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_document_comments_users_user_id",
                table: "document_comments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_chapters_chapter_id",
                table: "documents",
                column: "chapter_id",
                principalTable: "chapters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
