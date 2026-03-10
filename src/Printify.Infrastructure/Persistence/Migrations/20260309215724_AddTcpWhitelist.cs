using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTcpWhitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "tcp_whitelist_enabled",
                table: "workspaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tcp_whitelist_entries",
                table: "workspaces",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tcp_whitelist_enabled",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "tcp_whitelist_entries",
                table: "workspaces");
        }
    }
}
