using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTeamAndProjectToTaskTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Task_GeneralTask",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Task_GeneralTask",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_GeneralTask_ProjectId",
                table: "Task_GeneralTask",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_GeneralTask_TeamId",
                table: "Task_GeneralTask",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Project_Project_ProjectId",
                table: "Task_GeneralTask",
                column: "ProjectId",
                principalTable: "Project_Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Team_Team_TeamId",
                table: "Task_GeneralTask",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Project_Project_ProjectId",
                table: "Task_GeneralTask");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Team_Team_TeamId",
                table: "Task_GeneralTask");

            migrationBuilder.DropIndex(
                name: "IX_Task_GeneralTask_ProjectId",
                table: "Task_GeneralTask");

            migrationBuilder.DropIndex(
                name: "IX_Task_GeneralTask_TeamId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Task_GeneralTask");
        }
    }
}
