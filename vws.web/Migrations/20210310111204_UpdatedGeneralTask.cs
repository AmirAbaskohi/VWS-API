using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedGeneralTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask");

            migrationBuilder.AlterColumn<int>(
                name: "TaskStatusId",
                table: "Task_GeneralTask",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask",
                column: "TaskStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask");

            migrationBuilder.AlterColumn<int>(
                name: "TaskStatusId",
                table: "Task_GeneralTask",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskStatus_TaskStatusId",
                table: "Task_GeneralTask",
                column: "TaskStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
