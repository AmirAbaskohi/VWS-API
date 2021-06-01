using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedUserProjectAndUserTeamTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Project_UserProjectActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project_UserProjectActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_UserProjectActivity_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_UserProjectActivity_Project_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project_UserProjectOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project_UserProjectOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_UserProjectOrder_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_UserProjectOrder_Project_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Team_UserTeamActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team_UserTeamActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Team_UserTeamActivity_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Team_UserTeamActivity_Team_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team_Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Team_UserTeamOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team_UserTeamOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Team_UserTeamOrder_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Team_UserTeamOrder_Team_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team_Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Project_UserProjectActivity_ProjectId",
                table: "Project_UserProjectActivity",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_UserProjectActivity_UserProfileId",
                table: "Project_UserProjectActivity",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_UserProjectOrder_ProjectId",
                table: "Project_UserProjectOrder",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_UserProjectOrder_UserProfileId",
                table: "Project_UserProjectOrder",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_UserTeamActivity_TeamId",
                table: "Team_UserTeamActivity",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_UserTeamActivity_UserProfileId",
                table: "Team_UserTeamActivity",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_UserTeamOrder_TeamId",
                table: "Team_UserTeamOrder",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_UserTeamOrder_UserProfileId",
                table: "Team_UserTeamOrder",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Project_UserProjectActivity");

            migrationBuilder.DropTable(
                name: "Project_UserProjectOrder");

            migrationBuilder.DropTable(
                name: "Team_UserTeamActivity");

            migrationBuilder.DropTable(
                name: "Team_UserTeamOrder");
        }
    }
}
