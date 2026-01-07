using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "runtime_status",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "runtime_status_error",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "runtime_status_updated_at",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "target_status",
                table: "printers");

            migrationBuilder.AddColumn<int>(
                name: "bytes_received",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "bytes_sent",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bytes_received",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "bytes_sent",
                table: "documents");

            migrationBuilder.AddColumn<string>(
                name: "runtime_status",
                table: "printers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "runtime_status_error",
                table: "printers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "runtime_status_updated_at",
                table: "printers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_status",
                table: "printers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
