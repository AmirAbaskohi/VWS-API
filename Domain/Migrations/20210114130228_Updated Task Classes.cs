using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedTaskClasses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Task_GeneralTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Task_GeneralTask",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Task_GeneralTask",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Task_GeneralTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Task_GeneralTask",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "TaskScheduleTypeId",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Task_TaskScheduleType",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    NameMultiLang = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskScheduleType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_GeneralTask_TaskScheduleTypeId",
                table: "Task_GeneralTask",
                column: "TaskScheduleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskScheduleType_TaskScheduleTypeId",
                table: "Task_GeneralTask",
                column: "TaskScheduleTypeId",
                principalTable: "Task_TaskScheduleType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskScheduleType_TaskScheduleTypeId",
                table: "Task_GeneralTask");

            migrationBuilder.DropTable(
                name: "Task_TaskScheduleType");

            migrationBuilder.DropIndex(
                name: "IX_Task_GeneralTask_TaskScheduleTypeId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "TaskScheduleTypeId",
                table: "Task_GeneralTask");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Task_GeneralTask",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
