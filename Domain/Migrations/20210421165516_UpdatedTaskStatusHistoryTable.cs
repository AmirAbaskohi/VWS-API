using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTaskStatusHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskStatusHistories_Base_UserProfile_ChangeById",
                table: "TaskStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskStatusHistories_Task_GeneralTask_GeneralTaskId",
                table: "TaskStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskStatusHistories_Task_TaskStatus_LastStatusId",
                table: "TaskStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskStatusHistories_Task_TaskStatus_NewStatusId",
                table: "TaskStatusHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskStatusHistories",
                table: "TaskStatusHistories");

            migrationBuilder.RenameTable(
                name: "TaskStatusHistories",
                newName: "Task_TaskStatusHistory");

            migrationBuilder.RenameIndex(
                name: "IX_TaskStatusHistories_NewStatusId",
                table: "Task_TaskStatusHistory",
                newName: "IX_Task_TaskStatusHistory_NewStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskStatusHistories_LastStatusId",
                table: "Task_TaskStatusHistory",
                newName: "IX_Task_TaskStatusHistory_LastStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskStatusHistories_GeneralTaskId",
                table: "Task_TaskStatusHistory",
                newName: "IX_Task_TaskStatusHistory_GeneralTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskStatusHistories_ChangeById",
                table: "Task_TaskStatusHistory",
                newName: "IX_Task_TaskStatusHistory_ChangeById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TaskStatusHistory",
                table: "Task_TaskStatusHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskStatusHistory_Base_UserProfile_ChangeById",
                table: "Task_TaskStatusHistory",
                column: "ChangeById",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskStatusHistory",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_TaskStatus_LastStatusId",
                table: "Task_TaskStatusHistory",
                column: "LastStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_TaskStatus_NewStatusId",
                table: "Task_TaskStatusHistory",
                column: "NewStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskStatusHistory_Base_UserProfile_ChangeById",
                table: "Task_TaskStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_TaskStatus_LastStatusId",
                table: "Task_TaskStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskStatusHistory_Task_TaskStatus_NewStatusId",
                table: "Task_TaskStatusHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TaskStatusHistory",
                table: "Task_TaskStatusHistory");

            migrationBuilder.RenameTable(
                name: "Task_TaskStatusHistory",
                newName: "TaskStatusHistories");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskStatusHistory_NewStatusId",
                table: "TaskStatusHistories",
                newName: "IX_TaskStatusHistories_NewStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskStatusHistory_LastStatusId",
                table: "TaskStatusHistories",
                newName: "IX_TaskStatusHistories_LastStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskStatusHistory_GeneralTaskId",
                table: "TaskStatusHistories",
                newName: "IX_TaskStatusHistories_GeneralTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskStatusHistory_ChangeById",
                table: "TaskStatusHistories",
                newName: "IX_TaskStatusHistories_ChangeById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskStatusHistories",
                table: "TaskStatusHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskStatusHistories_Base_UserProfile_ChangeById",
                table: "TaskStatusHistories",
                column: "ChangeById",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskStatusHistories_Task_GeneralTask_GeneralTaskId",
                table: "TaskStatusHistories",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskStatusHistories_Task_TaskStatus_LastStatusId",
                table: "TaskStatusHistories",
                column: "LastStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskStatusHistories_Task_TaskStatus_NewStatusId",
                table: "TaskStatusHistories",
                column: "NewStatusId",
                principalTable: "Task_TaskStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }
    }
}
