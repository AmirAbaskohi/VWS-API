using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedCalenderHistoryTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Event",
                table: "Team_TeamHistory",
                newName: "EventBody");

            migrationBuilder.RenameColumn(
                name: "Event",
                table: "Task_TaskHistory",
                newName: "EventBody");

            migrationBuilder.RenameColumn(
                name: "Event",
                table: "Project_ProjectHistory",
                newName: "EventBody");

            migrationBuilder.CreateTable(
                name: "Calender_EventHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calender_EventHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calender_EventHistory_Calender_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Calender_Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calender_EventHistoryParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityParameterTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    EventHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShouldBeLocalized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calender_EventHistoryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calender_EventHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                        column: x => x.ActivityParameterTypeId,
                        principalTable: "ActivityParameterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calender_EventHistoryParameter_Calender_EventHistory_EventHistoryId",
                        column: x => x.EventHistoryId,
                        principalTable: "Calender_EventHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventHistory_EventId",
                table: "Calender_EventHistory",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventHistoryParameter_ActivityParameterTypeId",
                table: "Calender_EventHistoryParameter",
                column: "ActivityParameterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventHistoryParameter_EventHistoryId",
                table: "Calender_EventHistoryParameter",
                column: "EventHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calender_EventHistoryParameter");

            migrationBuilder.DropTable(
                name: "Calender_EventHistory");

            migrationBuilder.RenameColumn(
                name: "EventBody",
                table: "Team_TeamHistory",
                newName: "Event");

            migrationBuilder.RenameColumn(
                name: "EventBody",
                table: "Task_TaskHistory",
                newName: "Event");

            migrationBuilder.RenameColumn(
                name: "EventBody",
                table: "Project_ProjectHistory",
                newName: "Event");
        }
    }
}
