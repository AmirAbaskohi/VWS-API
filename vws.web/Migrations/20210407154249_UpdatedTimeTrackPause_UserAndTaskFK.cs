using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTimeTrackPause_UserAndTaskFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GeneralTaskId",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "UserProfileId",
                table: "Task_TimeTrackPause",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_GeneralTaskId",
                table: "Task_TimeTrackPause",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_UserProfileId",
                table: "Task_TimeTrackPause",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Base_UserProfile_UserProfileId",
                table: "Task_TimeTrackPause",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Task_GeneralTask_GeneralTaskId",
                table: "Task_TimeTrackPause",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Base_UserProfile_UserProfileId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Task_GeneralTask_GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_UserProfileId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "Task_TimeTrackPause");
        }
    }
}
