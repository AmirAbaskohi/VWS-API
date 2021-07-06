using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskStatusHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    ChangeById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskStatusHistories_Base_UserProfile_ChangeById",
                        column: x => x.ChangeById,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskStatusHistories_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskStatusHistories_Task_TaskStatus_LastStatusId",
                        column: x => x.LastStatusId,
                        principalTable: "Task_TaskStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_TaskStatusHistories_Task_TaskStatus_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "Task_TaskStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatusHistories_ChangeById",
                table: "TaskStatusHistories",
                column: "ChangeById");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatusHistories_GeneralTaskId",
                table: "TaskStatusHistories",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatusHistories_LastStatusId",
                table: "TaskStatusHistories",
                column: "LastStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatusHistories_NewStatusId",
                table: "TaskStatusHistories",
                column: "NewStatusId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskStatusHistories");
        }
    }
}
