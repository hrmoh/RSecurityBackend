using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleProject.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RAppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityUId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RChangeLogs_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RChangeLogs_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RChangeLogs_RAppUserId",
                table: "RChangeLogs",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RChangeLogs_WorkspaceId",
                table: "RChangeLogs",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RChangeLogs");
        }
    }
}
