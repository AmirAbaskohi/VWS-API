using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskArrachmentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_TaskCommentAttachment",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    FileId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskCommentAttachment", x => new { x.FileId, x.GeneralTaskId });
                    table.ForeignKey(
                        name: "FK_Task_TaskCommentAttachment_File_File_FileId1",
                        column: x => x.FileId1,
                        principalTable: "File_File",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Task_TaskCommentAttachment_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskCommentAttachment_FileId1",
                table: "Task_TaskCommentAttachment",
                column: "FileId1");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskCommentAttachment_GeneralTaskId",
                table: "Task_TaskCommentAttachment",
                column: "GeneralTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskCommentAttachment");
        }
    }
}
