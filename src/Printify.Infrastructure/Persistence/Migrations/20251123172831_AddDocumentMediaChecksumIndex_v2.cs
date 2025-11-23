using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentMediaChecksumIndex_v2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_document_media_checksum",
            table: "document_media",
            column: "checksum");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_document_media_checksum",
            table: "document_media");
    }
}
