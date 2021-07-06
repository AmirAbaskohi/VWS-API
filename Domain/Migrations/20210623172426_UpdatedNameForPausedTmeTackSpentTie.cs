using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedNameForPausedTmeTackSpentTie : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TimeTrackPauseSpentTime");

            migrationBuilder.CreateTable(
                name: "Task_TimeTrackPausedSpentTime",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    TotalTimeInMinutes = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TimeTrackPausedSpentTime", x => new { x.UserProfileId, x.GeneralTaskId });
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPausedSpentTime_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPausedSpentTime_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPausedSpentTime_GeneralTaskId",
                table: "Task_TimeTrackPausedSpentTime",
                column: "GeneralTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TimeTrackPausedSpentTime");

            migrationBuilder.CreateTable(
                name: "Task_TimeTrackPauseSpentTime",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    TotalTimeInMinutes = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TimeTrackPauseSpentTime", x => new { x.UserProfileId, x.GeneralTaskId });
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPauseSpentTime_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPauseSpentTime_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPauseSpentTime_GeneralTaskId",
                table: "Task_TimeTrackPauseSpentTime",
                column: "GeneralTaskId");
        }
    }
}
