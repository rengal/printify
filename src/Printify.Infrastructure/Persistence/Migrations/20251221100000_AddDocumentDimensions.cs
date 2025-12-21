using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "height_in_dots",
                table: "documents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "width_in_dots",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE documents
SET width_in_dots = COALESCE((SELECT width_in_dots FROM printers WHERE printers.id = documents.printer_id), width_in_dots),
    height_in_dots = (SELECT height_in_dots FROM printers WHERE printers.id = documents.printer_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "height_in_dots",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "width_in_dots",
                table: "documents");
        }
    }
}
