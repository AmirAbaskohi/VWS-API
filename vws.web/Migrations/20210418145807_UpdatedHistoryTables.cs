using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedHistoryTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "Task_TaskHistory",
                newName: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamHistory_TeamId",
                table: "Team_TeamHistory",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskHistory_GeneralTaskId",
                table: "Task_TaskHistory",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectHistory_ProjectId",
                table: "Project_ProjectHistory",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_ProjectHistory_Project_Project_ProjectId",
                table: "Project_ProjectHistory",
                column: "ProjectId",
                principalTable: "Project_Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskHistory_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskHistory",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Team_TeamHistory_Team_Team_TeamId",
                table: "Team_TeamHistory",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_ProjectHistory_Project_Project_ProjectId",
                table: "Project_ProjectHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskHistory_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Team_TeamHistory_Team_Team_TeamId",
                table: "Team_TeamHistory");

            migrationBuilder.DropIndex(
                name: "IX_Team_TeamHistory_TeamId",
                table: "Team_TeamHistory");

            migrationBuilder.DropIndex(
                name: "IX_Task_TaskHistory_GeneralTaskId",
                table: "Task_TaskHistory");

            migrationBuilder.DropIndex(
                name: "IX_Project_ProjectHistory_ProjectId",
                table: "Project_ProjectHistory");

            migrationBuilder.RenameColumn(
                name: "GeneralTaskId",
                table: "Task_TaskHistory",
                newName: "TaskId");
        }
    }
}
