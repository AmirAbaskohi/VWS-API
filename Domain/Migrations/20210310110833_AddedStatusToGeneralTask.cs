using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedStatusToGeneralTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskStatusId",
                table: "Task_GeneralTask",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_GeneralTask_TaskStatusId",
                table: "Task_GeneralTask",
                column: "TaskStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask",
                column: "TaskStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask");

            migrationBuilder.DropIndex(
                name: "IX_Task_GeneralTask_TaskStatusId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "TaskStatusId",
                table: "Task_GeneralTask");
        }
    }
}
