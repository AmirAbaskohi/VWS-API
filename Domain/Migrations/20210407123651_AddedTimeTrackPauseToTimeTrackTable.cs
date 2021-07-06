using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTimeTrackPauseToTimeTrackTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TimeTrackId",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_TimeTrackId",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId",
                principalTable: "Task_TimeTrack",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_TimeTrackId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "TimeTrackId",
                table: "Task_TimeTrackPause");
        }
    }
}
