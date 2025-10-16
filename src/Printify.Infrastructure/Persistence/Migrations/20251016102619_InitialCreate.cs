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
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "anonymous_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_from_ip = table.Column<string>(type: "TEXT", nullable: false),
                    linked_user_id = table.Column<Guid>(type: "TEXT", nullable: true)
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
                name: "IX_anonymous_sessions_linked_user_id",
                table: "anonymous_sessions",
                column: "linked_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anonymous_sessions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
