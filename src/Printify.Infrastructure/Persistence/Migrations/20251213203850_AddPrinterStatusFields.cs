using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "desired_status",
                table: "printers",
                type: "TEXT",
                nullable: false,
                defaultValue: "Started");

            migrationBuilder.AddColumn<string>(
                name: "runtime_status",
                table: "printers",
                type: "TEXT",
                nullable: false,
                defaultValue: "Unknown");

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

            migrationBuilder.Sql("""
UPDATE printers
SET desired_status = COALESCE(NULLIF(desired_status, ''), 'Started'),
    runtime_status = COALESCE(NULLIF(runtime_status, ''), 'Unknown')
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "desired_status",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "runtime_status",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "runtime_status_error",
                table: "printers");

            migrationBuilder.DropColumn(
                name: "runtime_status_updated_at",
                table: "printers");
        }
    }
}
