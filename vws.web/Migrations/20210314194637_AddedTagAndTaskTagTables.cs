using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTagAndTaskTagTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_Tag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_Tag_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Task_Tag_Project_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Task_Tag_Team_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team_Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Task_TaskTag",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskTag", x => new { x.TagId, x.GeneralTaskId });
                    table.ForeignKey(
                        name: "FK_Task_TaskTag_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TaskTag_Task_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Task_Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_Tag_ProjectId",
                table: "Task_Tag",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_Tag_TeamId",
                table: "Task_Tag",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_Tag_UserProfileId",
                table: "Task_Tag",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskTag_GeneralTaskId",
                table: "Task_TaskTag",
                column: "GeneralTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskTag");

            migrationBuilder.DropTable(
                name: "Task_Tag");
        }
    }
}
