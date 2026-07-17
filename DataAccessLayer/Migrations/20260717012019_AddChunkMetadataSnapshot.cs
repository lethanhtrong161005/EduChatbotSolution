using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkMetadataSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "chunk_overlap",
                table: "chunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "chunk_size",
                table: "chunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // New FileType:
            // TXT=0, HTML=1, PDF=2, DOCX=3, XLSX=4, PPTX=5, Other=6
            migrationBuilder.Sql(
                """
                UPDATE "documents"
                SET "file_type" = CASE
                    WHEN lower("original_file_name") LIKE '%.txt'  THEN 0
                    WHEN lower("original_file_name") LIKE '%.html' THEN 1
                    WHEN lower("original_file_name") LIKE '%.pdf'  THEN 2
                    WHEN lower("original_file_name") LIKE '%.docx' THEN 3
                    WHEN lower("original_file_name") LIKE '%.xlsx' THEN 4
                    WHEN lower("original_file_name") LIKE '%.pptx' THEN 5
                    ELSE 6
                END;
                """);

            // Existing Supabase document locators contain only the object key.
            migrationBuilder.Sql(
                """
                UPDATE "documents"
                SET "storage_locator" =
                    CASE
                        WHEN "storage_locator" LIKE 'documents/%'
                            THEN "storage_locator"
                        ELSE 'documents/' || ltrim("storage_locator", '/')
                    END
                WHERE "storage_method" = 2
                AND "storage_locator" IS NOT NULL
                AND btrim("storage_locator") <> '';
                """);

            // Old user-import objects were stored in the documents bucket.
            // The corresponding objects must be moved manually to user_imports,
            // preserving the object key.
            migrationBuilder.Sql(
                """
                UPDATE "user_import_batches"
                SET "storage_locator" =
                    CASE
                        WHEN "storage_locator" LIKE 'user_imports/%'
                            THEN "storage_locator"
                        WHEN "storage_locator" LIKE 'documents/%'
                            THEN 'user_imports/' ||
                                substring(
                                    "storage_locator"
                                    FROM length('documents/') + 1
                                )
                        ELSE 'user_imports/' || ltrim("storage_locator", '/')
                    END
                WHERE "storage_locator" IS NOT NULL
                AND btrim("storage_locator") <> '';
                """);

            // Preserve valid existing chunk metadata. Zero intentionally forces
            // re-chunking where the document did not record its prior configuration.
            migrationBuilder.Sql(
                """
                UPDATE "chunks" AS c
                SET "chunk_size" = COALESCE(d."indexed_chunk_size", 0),
                    "chunk_overlap" = COALESCE(d."indexed_chunk_overlap", 0)
                FROM "documents" AS d
                WHERE d."id" = c."document_id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "documents"
                SET "storage_locator" =
                    substring(
                        "storage_locator"
                        FROM length('documents/') + 1
                    )
                WHERE "storage_method" = 2
                AND "storage_locator" LIKE 'documents/%';
                """);

            // Old DocumentType:
            // TXT=0, DOCX=1, PDF=2, HTML=3, PPTX=4, Other=5
            migrationBuilder.Sql(
                """
                UPDATE "documents"
                SET "file_type" = CASE
                    WHEN lower("original_file_name") LIKE '%.txt'  THEN 0
                    WHEN lower("original_file_name") LIKE '%.docx' THEN 1
                    WHEN lower("original_file_name") LIKE '%.pdf'  THEN 2
                    WHEN lower("original_file_name") LIKE '%.html' THEN 3
                    WHEN lower("original_file_name") LIKE '%.pptx' THEN 4
                    ELSE 5
                END;
                """);


            migrationBuilder.DropColumn(
                name: "chunk_overlap",
                table: "chunks");

            migrationBuilder.DropColumn(
                name: "chunk_size",
                table: "chunks");
        }
    }
}
