using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_TaskHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Task_TaskHistoryParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityParameterTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    TaskHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskHistoryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TaskHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                        column: x => x.ActivityParameterTypeId,
                        principalTable: "ActivityParameterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TaskHistoryParameter_Task_TaskHistory_TaskHistoryId",
                        column: x => x.TaskHistoryId,
                        principalTable: "Task_TaskHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskHistoryParameter_ActivityParameterTypeId",
                table: "Task_TaskHistoryParameter",
                column: "ActivityParameterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskHistoryParameter_TaskHistoryId",
                table: "Task_TaskHistoryParameter",
                column: "TaskHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskHistoryParameter");

            migrationBuilder.DropTable(
                name: "Task_TaskHistory");
        }
    }
}
