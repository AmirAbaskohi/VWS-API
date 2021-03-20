using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdateForAttachemtn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCommentAttachment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.RenameColumn(
                name: "GeneralTaskId",
                table: "Task_TaskCommentAttachment",
                newName: "TaskCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskCommentAttachment_GeneralTaskId",
                table: "Task_TaskCommentAttachment",
                newName: "IX_Task_TaskCommentAttachment_TaskCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCommentAttachment_Task_TaskComment_TaskCommentId",
                table: "Task_TaskCommentAttachment",
                column: "TaskCommentId",
                principalTable: "Task_TaskComment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCommentAttachment_Task_TaskComment_TaskCommentId",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.RenameColumn(
                name: "TaskCommentId",
                table: "Task_TaskCommentAttachment",
                newName: "GeneralTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskCommentAttachment_TaskCommentId",
                table: "Task_TaskCommentAttachment",
                newName: "IX_Task_TaskCommentAttachment_GeneralTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCommentAttachment_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskCommentAttachment",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
