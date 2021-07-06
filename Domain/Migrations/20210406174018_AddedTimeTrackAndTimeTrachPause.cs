using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTimeTrackAndTimeTrachPause : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Task_TimeTrack",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalTimeInMinutes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TimeTrack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrack_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrack_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Task_TimeTrackPause",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    TotalTimeInMinutes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TimeTrackPause", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPause_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Task_TimeTrackPause_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrack_GeneralTaskId",
                table: "Task_TimeTrack",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrack_UserProfileId",
                table: "Task_TimeTrack",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_GeneralTaskId",
                table: "Task_TimeTrackPause",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_UserProfileId",
                table: "Task_TimeTrackPause",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TimeTrack");

            migrationBuilder.DropTable(
                name: "Task_TimeTrackPause");
        }
    }
}
