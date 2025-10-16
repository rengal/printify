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
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
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
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    listen_tcp_port_number = table.Column<int>(type: "INTEGER", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "anonymous_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    linked_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_printers_display_name",
                table: "printers",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_sessions_linked_user_id",
                table: "anonymous_sessions",
                column: "linked_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printers");

            migrationBuilder.DropTable(
                name: "anonymous_sessions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
