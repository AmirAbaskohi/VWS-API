using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedEventTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventUser",
                table: "Calender_EventUser");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Calender_Event");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Calender_EventUser",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Calender_EventUser",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Calender_EventUser",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventUser",
                table: "Calender_EventUser",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventUser_UserProfileId",
                table: "Calender_EventUser",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventUser",
                table: "Calender_EventUser");

            migrationBuilder.DropIndex(
                name: "IX_Calender_EventUser_UserProfileId",
                table: "Calender_EventUser");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Calender_EventUser");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Calender_EventUser");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Calender_EventUser");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Calender_Event",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventUser",
                table: "Calender_EventUser",
                columns: new[] { "UserProfileId", "EventId" });
        }
    }
}
