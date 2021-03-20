using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedCommentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskComment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskComment");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Task_TaskComment");

            migrationBuilder.AlterColumn<long>(
                name: "GeneralTaskId",
                table: "Task_TaskComment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskComment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskComment",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskComment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskComment");

            migrationBuilder.AlterColumn<long>(
                name: "GeneralTaskId",
                table: "Task_TaskComment",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "TaskId",
                table: "Task_TaskComment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskComment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskComment",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
