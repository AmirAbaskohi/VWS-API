using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTimeTrackPauseTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Base_UserProfile_UserProfileId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Task_GeneralTask_GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TimeTrackPause",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_TimeTrackId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropIndex(
                name: "IX_Task_TimeTrackPause_UserProfileId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "GeneralTaskId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "TotalTimeInMinutes",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "Task_TimeTrackPause");

            migrationBuilder.AlterColumn<long>(
                name: "TimeTrackId",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TimeTrackPause",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId",
                principalTable: "Task_TimeTrack",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Task_TimeTrackPause",
                table: "Task_TimeTrackPause");

            migrationBuilder.AlterColumn<long>(
                name: "TimeTrackId",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "GeneralTaskId",
                table: "Task_TimeTrackPause",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalTimeInMinutes",
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

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task_TimeTrackPause",
                table: "Task_TimeTrackPause",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_GeneralTaskId",
                table: "Task_TimeTrackPause",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TimeTrackPause_TimeTrackId",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId");

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
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Task_GeneralTask_GeneralTaskId",
                table: "Task_TimeTrackPause",
                column: "GeneralTaskId",
                principalTable: "Task_GeneralTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TimeTrackPause_Task_TimeTrack_TimeTrackId",
                table: "Task_TimeTrackPause",
                column: "TimeTrackId",
                principalTable: "Task_TimeTrack",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
