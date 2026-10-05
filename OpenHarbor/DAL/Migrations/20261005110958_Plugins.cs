using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHarbor.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Plugins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RouteSubpath = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false, collation: "NOCASE"),
                    DllRelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PublicFolderRelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDashboardProvider = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSelectedDashboardProvider = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_IsSelectedDashboardProvider",
                table: "Plugins",
                column: "IsSelectedDashboardProvider",
                unique: true,
                filter: "[IsSelectedDashboardProvider] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_RouteSubpath",
                table: "Plugins",
                column: "RouteSubpath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Plugins");
        }
    }
}
