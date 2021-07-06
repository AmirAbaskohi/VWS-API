using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class MakeAllChangesUpToNow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.AlterColumn<byte>(
                name: "TaskPriorityId",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask",
                column: "TaskPriorityId",
                principalTable: "Task_TaskPriority",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.AlterColumn<byte>(
                name: "TaskPriorityId",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<byte>(
                name: "PriorityId",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask",
                column: "TaskPriorityId",
                principalTable: "Task_TaskPriority",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
