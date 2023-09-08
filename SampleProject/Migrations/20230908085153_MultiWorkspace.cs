using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleProject.Migrations
{
    /// <inheritdoc />
    public partial class MultiWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WokspaceOrder = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RWSRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RWSUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RAppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InviteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MemberFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSUser_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUser_RWorkspaces_RWorkspaceId",
                        column: x => x.RWorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RWSPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurableItemShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperationShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RWSRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSPermissions_RWSRoles_RWSRoleId",
                        column: x => x.RWSRoleId,
                        principalTable: "RWSRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RWSUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_RWSRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RWSRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RWSPermissions_RWSRoleId",
                table: "RWSPermissions",
                column: "RWSRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUser_RAppUserId",
                table: "RWSUser",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUser_RWorkspaceId",
                table: "RWSUser",
                column: "RWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_RoleId",
                table: "RWSUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_UserId",
                table: "RWSUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_WorkspaceId",
                table: "RWSUserRoles",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RWSPermissions");

            migrationBuilder.DropTable(
                name: "RWSUser");

            migrationBuilder.DropTable(
                name: "RWSUserRoles");

            migrationBuilder.DropTable(
                name: "RWSRoles");

            migrationBuilder.DropTable(
                name: "RWorkspaces");
        }
    }
}
