using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    print_job_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    printer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    protocol = table.Column<string>(type: "TEXT", nullable: false),
                    client_address = table.Column<string>(type: "TEXT", nullable: true)
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
                    owner_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    owner_anonymous_session_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    protocol = table.Column<string>(type: "TEXT", nullable: false),
                    width_in_dots = table.Column<int>(type: "INTEGER", nullable: false),
                    height_in_dots = table.Column<int>(type: "INTEGER", nullable: true),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    listen_tcp_port_number = table.Column<int>(type: "INTEGER", nullable: false),
                    emulate_buffer_capacity = table.Column<bool>(type: "INTEGER", nullable: false),
                    buffer_drain_rate = table.Column<decimal>(type: "TEXT", nullable: true),
                    buffer_max_capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    is_pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_elements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_elements", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_elements_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "anonymous_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    linked_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anonymous_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_anonymous_sessions_users_linked_user_id",
                        column: x => x.linked_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    document_element_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_type = table.Column<string>(type: "TEXT", nullable: false),
                    length = table.Column<long>(type: "INTEGER", nullable: true),
                    checksum = table.Column<string>(type: "TEXT", nullable: true),
                    url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_media", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_media_document_elements_document_element_id",
                        column: x => x.document_element_id,
                        principalTable: "document_elements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_sessions_linked_user_id",
                table: "anonymous_sessions",
                column: "linked_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_elements_document_id_sequence",
                table: "document_elements",
                columns: new[] { "document_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_media_checksum",
                table: "document_media",
                column: "checksum");

            migrationBuilder.CreateIndex(
                name: "IX_document_media_document_element_id",
                table: "document_media",
                column: "document_element_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_printer_created_at_id",
                table: "documents",
                columns: new[] { "printer_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_printers_display_name",
                table: "printers",
                column: "display_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anonymous_sessions");

            migrationBuilder.DropTable(
                name: "document_media");

            migrationBuilder.DropTable(
                name: "print_jobs");

            migrationBuilder.DropTable(
                name: "printers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "document_elements");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
