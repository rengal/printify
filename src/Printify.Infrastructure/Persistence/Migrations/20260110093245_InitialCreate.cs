using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    owner_workspace_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_type = table.Column<string>(type: "TEXT", nullable: false),
                    length = table.Column<long>(type: "INTEGER", nullable: false),
                    checksum = table.Column<string>(type: "TEXT", nullable: false),
                    file_name = table.Column<string>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_media", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    print_job_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    printer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    width_in_dots = table.Column<int>(type: "INTEGER", nullable: false),
                    height_in_dots = table.Column<int>(type: "INTEGER", nullable: true),
                    protocol = table.Column<string>(type: "TEXT", nullable: false),
                    client_address = table.Column<string>(type: "TEXT", nullable: true),
                    bytes_received = table.Column<int>(type: "INTEGER", nullable: false),
                    bytes_sent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "print_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    printer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    protocol = table.Column<string>(type: "TEXT", nullable: false),
                    width_in_dots = table.Column<int>(type: "INTEGER", nullable: false),
                    height_in_dots = table.Column<int>(type: "INTEGER", nullable: true),
                    client_address = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "printers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    owner_workspace_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    protocol = table.Column<string>(type: "TEXT", nullable: false),
                    width_in_dots = table.Column<int>(type: "INTEGER", nullable: false),
                    height_in_dots = table.Column<int>(type: "INTEGER", nullable: true),
                    listen_tcp_port_number = table.Column<int>(type: "INTEGER", nullable: false),
                    emulate_buffer_capacity = table.Column<bool>(type: "INTEGER", nullable: false),
                    buffer_drain_rate = table.Column<decimal>(type: "TEXT", nullable: true),
                    buffer_max_capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    is_pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_viewed_document_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_document_received_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    token = table.Column<string>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    document_retention_days = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_elements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: false),
                    command_raw = table.Column<string>(type: "TEXT", nullable: false),
                    length_in_bytes = table.Column<int>(type: "INTEGER", nullable: false),
                    media_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_elements", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_elements_document_media_media_id",
                        column: x => x.media_id,
                        principalTable: "document_media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_elements_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "printer_operational_flags",
                columns: table => new
                {
                    printer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    target_state = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    cover_open = table.Column<bool>(type: "INTEGER", nullable: false),
                    paper_out = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_offline = table.Column<bool>(type: "INTEGER", nullable: false),
                    has_error = table.Column<bool>(type: "INTEGER", nullable: false),
                    paper_near_end = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_operational_flags", x => x.printer_id);
                    table.ForeignKey(
                        name: "FK_printer_operational_flags_printers_printer_id",
                        column: x => x.printer_id,
                        principalTable: "printers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_elements_document_id_sequence",
                table: "document_elements",
                columns: new[] { "document_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_elements_media_id",
                table: "document_elements",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_media_checksum",
                table: "document_media",
                column: "checksum");

            migrationBuilder.CreateIndex(
                name: "IX_documents_printer_created_at_id",
                table: "documents",
                columns: new[] { "printer_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_printers_display_name",
                table: "printers",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "IX_printers_listen_tcp_port_number",
                table: "printers",
                column: "listen_tcp_port_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_elements");

            migrationBuilder.DropTable(
                name: "print_jobs");

            migrationBuilder.DropTable(
                name: "printer_operational_flags");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "document_media");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "printers");
        }
    }
}
