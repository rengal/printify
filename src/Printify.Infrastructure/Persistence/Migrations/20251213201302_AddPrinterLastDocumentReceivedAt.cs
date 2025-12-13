using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterLastDocumentReceivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_document_received_at",
                table: "printers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "document_media",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_workspace_id",
                table: "document_media",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_document_received_at",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "file_name",
                table: "document_media");

            migrationBuilder.DropColumn(
                name: "owner_workspace_id",
                table: "document_media");
        }
    }
}
