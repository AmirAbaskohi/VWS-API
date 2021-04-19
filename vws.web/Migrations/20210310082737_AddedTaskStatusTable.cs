using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskStatusTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_TaskStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TaskStatus_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TaskStatus_Project_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TaskStatus_Team_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team_Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskStatus_ProjectId",
                table: "Task_TaskStatus",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskStatus_TeamId",
                table: "Task_TaskStatus",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskStatus_UserProfileId",
                table: "Task_TaskStatus",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskStatus");
        }
    }
}
