using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTableNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReads_Chat_Message_MessageId",
                table: "MessageReads");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskCheckListItems_TaskCheckLists_TaskCheckListId",
                table: "TaskCheckListItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskCheckLists_Task_GeneralTask_GeneralTaskId",
                table: "TaskCheckLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskCommentTemplates",
                table: "TaskCommentTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskCheckLists",
                table: "TaskCheckLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskCheckListItems",
                table: "TaskCheckListItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageReads",
                table: "MessageReads");

            migrationBuilder.RenameTable(
                name: "TaskCommentTemplates",
                newName: "Task_TaskCommentTemplate");

            migrationBuilder.RenameTable(
                name: "TaskCheckLists",
                newName: "Task_TaskCheckList");

            migrationBuilder.RenameTable(
                name: "TaskCheckListItems",
                newName: "Task_TaskCheckListItem");

            migrationBuilder.RenameTable(
                name: "MessageReads",
                newName: "Chat_MessageRead");

            migrationBuilder.RenameIndex(
                name: "IX_TaskCheckLists_GeneralTaskId",
                table: "Task_TaskCheckList",
                newName: "IX_Task_TaskCheckList_GeneralTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskCheckListItems_TaskCheckListId",
                table: "Task_TaskCheckListItem",
                newName: "IX_Task_TaskCheckListItem_TaskCheckListId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageReads_MessageId",
                table: "Chat_MessageRead",
                newName: "IX_Chat_MessageRead_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TaskCommentTemplate",
                table: "Task_TaskCommentTemplate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TaskCheckList",
                table: "Task_TaskCheckList",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TaskCheckListItem",
                table: "Task_TaskCheckListItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chat_MessageRead",
                table: "Chat_MessageRead",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chat_MessageRead_Chat_Message_MessageId",
                table: "Chat_MessageRead",
                column: "MessageId",
                principalTable: "Chat_Message",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCheckList_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskCheckList",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCheckListItem_Task_TaskCheckList_TaskCheckListId",
                table: "Task_TaskCheckListItem",
                column: "TaskCheckListId",
                principalTable: "Task_TaskCheckList",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chat_MessageRead_Chat_Message_MessageId",
                table: "Chat_MessageRead");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCheckList_Task_GeneralTask_GeneralTaskId",
                table: "Task_TaskCheckList");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCheckListItem_Task_TaskCheckList_TaskCheckListId",
                table: "Task_TaskCheckListItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TaskCommentTemplate",
                table: "Task_TaskCommentTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TaskCheckListItem",
                table: "Task_TaskCheckListItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TaskCheckList",
                table: "Task_TaskCheckList");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chat_MessageRead",
                table: "Chat_MessageRead");

            migrationBuilder.RenameTable(
                name: "Task_TaskCommentTemplate",
                newName: "TaskCommentTemplates");

            migrationBuilder.RenameTable(
                name: "Task_TaskCheckListItem",
                newName: "TaskCheckListItems");

            migrationBuilder.RenameTable(
                name: "Task_TaskCheckList",
                newName: "TaskCheckLists");

            migrationBuilder.RenameTable(
                name: "Chat_MessageRead",
                newName: "MessageReads");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskCheckListItem_TaskCheckListId",
                table: "TaskCheckListItems",
                newName: "IX_TaskCheckListItems_TaskCheckListId");

            migrationBuilder.RenameIndex(
                name: "IX_Task_TaskCheckList_GeneralTaskId",
                table: "TaskCheckLists",
                newName: "IX_TaskCheckLists_GeneralTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Chat_MessageRead_MessageId",
                table: "MessageReads",
                newName: "IX_MessageReads_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskCommentTemplates",
                table: "TaskCommentTemplates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskCheckListItems",
                table: "TaskCheckListItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskCheckLists",
                table: "TaskCheckLists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageReads",
                table: "MessageReads",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageReads_Chat_Message_MessageId",
                table: "MessageReads",
                column: "MessageId",
                principalTable: "Chat_Message",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskCheckListItems_TaskCheckLists_TaskCheckListId",
                table: "TaskCheckListItems",
                column: "TaskCheckListId",
                principalTable: "TaskCheckLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskCheckLists_Task_GeneralTask_GeneralTaskId",
                table: "TaskCheckLists",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
