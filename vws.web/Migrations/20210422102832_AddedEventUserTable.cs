using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedEventUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calender_EventUser",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calender_EventUser", x => new { x.UserProfileId, x.EventId });
                    table.ForeignKey(
                        name: "FK_Calender_EventUser_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calender_EventUser_Calender_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Calender_Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventUser_EventId",
                table: "Calender_EventUser",
                column: "EventId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calender_EventUser");
        }
    }
}
