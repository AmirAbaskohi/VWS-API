using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace vws.web.Migrations
{
    public partial class AddedSettingsColumnsInProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "FirstCalendarTypeId",
                table: "Base_UserProfile",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<byte>(
                name: "FirstWeekDayId",
                table: "Base_UserProfile",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)2);

            migrationBuilder.AddColumn<bool>(
                name: "IsDarkModeOn",
                table: "Base_UserProfile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSecondCalendarOn",
                table: "Base_UserProfile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "SecondCalendarTypeId",
                table: "Base_UserProfile",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "ZoomRatio",
                table: "Base_UserProfile",
                type: "real",
                nullable: false,
                defaultValue: 0.85f);

            migrationBuilder.CreateTable(
                name: "Base_UserWeekends",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekDayId = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Base_UserWeekends", x => new { x.WeekDayId, x.UserProfileId });
                    table.ForeignKey(
                        name: "FK_Base_UserWeekends_Base_DayOfWeek_WeekDayId",
                        column: x => x.WeekDayId,
                        principalTable: "Base_DayOfWeek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Base_UserWeekends_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserProfile_FirstCalendarTypeId",
                table: "Base_UserProfile",
                column: "FirstCalendarTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserProfile_FirstWeekDayId",
                table: "Base_UserProfile",
                column: "FirstWeekDayId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserProfile_SecondCalendarTypeId",
                table: "Base_UserProfile",
                column: "SecondCalendarTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserWeekends_UserProfileId",
                table: "Base_UserWeekends",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_Base_CalendarType_FirstCalendarTypeId",
                table: "Base_UserProfile",
                column: "FirstCalendarTypeId",
                principalTable: "Base_CalendarType",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_Base_CalendarType_SecondCalendarTypeId",
                table: "Base_UserProfile",
                column: "SecondCalendarTypeId",
                principalTable: "Base_CalendarType",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_Base_DayOfWeek_FirstWeekDayId",
                table: "Base_UserProfile",
                column: "FirstWeekDayId",
                principalTable: "Base_DayOfWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_Base_CalendarType_FirstCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_Base_CalendarType_SecondCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_Base_DayOfWeek_FirstWeekDayId",
                table: "Base_UserProfile");

            migrationBuilder.DropTable(
                name: "Base_UserWeekends");

            migrationBuilder.DropIndex(
                name: "IX_Base_UserProfile_FirstCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_Base_UserProfile_FirstWeekDayId",
                table: "Base_UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_Base_UserProfile_SecondCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "FirstCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "FirstWeekDayId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "IsDarkModeOn",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "IsSecondCalendarOn",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "SecondCalendarTypeId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "ZoomRatio",
                table: "Base_UserProfile");
        }
    }
}
