using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskAttachmentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_TaskAttachment",
                columns: table => new
                {
                    FileContainerId = table.Column<int>(type: "int", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    FileContainerGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskAttachment", x => new { x.FileContainerId, x.GeneralTaskId });
                    table.ForeignKey(
                        name: "FK_Task_TaskAttachment_File_FileContainer_FileContainerId",
                        column: x => x.FileContainerId,
                        principalTable: "File_FileContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TaskAttachment_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskAttachment_GeneralTaskId",
                table: "Task_TaskAttachment",
                column: "GeneralTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskAttachment");
        }
    }
}
