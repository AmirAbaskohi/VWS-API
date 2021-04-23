using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedEventUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calender_EventUser");

            migrationBuilder.CreateTable(
                name: "Calender_EventMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calender_EventMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calender_EventMember_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calender_EventMember_Calender_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Calender_Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventMember_EventId",
                table: "Calender_EventMember",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventMember_UserProfileId",
                table: "Calender_EventMember",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calender_EventMember");

            migrationBuilder.CreateTable(
                name: "Calender_EventUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calender_EventUser", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_Calender_EventUser_UserProfileId",
                table: "Calender_EventUser",
                column: "UserProfileId");
        }
    }
}
