using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTaskCommentAttachment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCommentAttachment_File_File_FileId1",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.DropIndex(
                name: "IX_Task_TaskCommentAttachment_FileId1",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.DropColumn(
                name: "FileId1",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Task_TaskCommentAttachment",
                newName: "FileContainerId");

            migrationBuilder.AddColumn<Guid>(
                name: "FileContainerGuid",
                table: "Task_TaskCommentAttachment",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCommentAttachment_File_FileContainer_FileContainerId",
                table: "Task_TaskCommentAttachment",
                column: "FileContainerId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskCommentAttachment_File_FileContainer_FileContainerId",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.DropColumn(
                name: "FileContainerGuid",
                table: "Task_TaskCommentAttachment");

            migrationBuilder.RenameColumn(
                name: "FileContainerId",
                table: "Task_TaskCommentAttachment",
                newName: "FileId");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId1",
                table: "Task_TaskCommentAttachment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskCommentAttachment_FileId1",
                table: "Task_TaskCommentAttachment",
                column: "FileId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskCommentAttachment_File_File_FileId1",
                table: "Task_TaskCommentAttachment",
                column: "FileId1",
                principalTable: "File_File",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
