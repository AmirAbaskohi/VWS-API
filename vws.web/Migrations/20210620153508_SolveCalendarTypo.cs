using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class SolveCalendarTypo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calender_Event_Team_Team_TeamId",
                table: "Calender_Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventHistory_Calender_Event_EventId",
                table: "Calender_EventHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                table: "Calender_EventHistoryParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventHistoryParameter_Calender_EventHistory_EventHistoryId",
                table: "Calender_EventHistoryParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventMember_Base_UserProfile_UserProfileId",
                table: "Calender_EventMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventMember_Calender_Event_EventId",
                table: "Calender_EventMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventProject_Calender_Event_EventId",
                table: "Calender_EventProject");

            migrationBuilder.DropForeignKey(
                name: "FK_Calender_EventProject_Project_Project_ProjectId",
                table: "Calender_EventProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventProject",
                table: "Calender_EventProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventMember",
                table: "Calender_EventMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventHistoryParameter",
                table: "Calender_EventHistoryParameter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_EventHistory",
                table: "Calender_EventHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calender_Event",
                table: "Calender_Event");

            migrationBuilder.RenameTable(
                name: "Calender_EventProject",
                newName: "Calendar_EventProject");

            migrationBuilder.RenameTable(
                name: "Calender_EventMember",
                newName: "Calendar_EventMember");

            migrationBuilder.RenameTable(
                name: "Calender_EventHistoryParameter",
                newName: "Calendar_EventHistoryParameter");

            migrationBuilder.RenameTable(
                name: "Calender_EventHistory",
                newName: "Calendar_EventHistory");

            migrationBuilder.RenameTable(
                name: "Calender_Event",
                newName: "Calendar_Event");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventProject_EventId",
                table: "Calendar_EventProject",
                newName: "IX_Calendar_EventProject_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventMember_UserProfileId",
                table: "Calendar_EventMember",
                newName: "IX_Calendar_EventMember_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventMember_EventId",
                table: "Calendar_EventMember",
                newName: "IX_Calendar_EventMember_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventHistoryParameter_EventHistoryId",
                table: "Calendar_EventHistoryParameter",
                newName: "IX_Calendar_EventHistoryParameter_EventHistoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventHistoryParameter_ActivityParameterTypeId",
                table: "Calendar_EventHistoryParameter",
                newName: "IX_Calendar_EventHistoryParameter_ActivityParameterTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_EventHistory_EventId",
                table: "Calendar_EventHistory",
                newName: "IX_Calendar_EventHistory_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calender_Event_TeamId",
                table: "Calendar_Event",
                newName: "IX_Calendar_Event_TeamId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar_EventProject",
                table: "Calendar_EventProject",
                columns: new[] { "ProjectId", "EventId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar_EventMember",
                table: "Calendar_EventMember",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar_EventHistoryParameter",
                table: "Calendar_EventHistoryParameter",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar_EventHistory",
                table: "Calendar_EventHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calendar_Event",
                table: "Calendar_Event",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_Event_Team_Team_TeamId",
                table: "Calendar_Event",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventHistory_Calendar_Event_EventId",
                table: "Calendar_EventHistory",
                column: "EventId",
                principalTable: "Calendar_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                table: "Calendar_EventHistoryParameter",
                column: "ActivityParameterTypeId",
                principalTable: "ActivityParameterType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventHistoryParameter_Calendar_EventHistory_EventHistoryId",
                table: "Calendar_EventHistoryParameter",
                column: "EventHistoryId",
                principalTable: "Calendar_EventHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventMember_Base_UserProfile_UserProfileId",
                table: "Calendar_EventMember",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventMember_Calendar_Event_EventId",
                table: "Calendar_EventMember",
                column: "EventId",
                principalTable: "Calendar_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventProject_Calendar_Event_EventId",
                table: "Calendar_EventProject",
                column: "EventId",
                principalTable: "Calendar_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calendar_EventProject_Project_Project_ProjectId",
                table: "Calendar_EventProject",
                column: "ProjectId",
                principalTable: "Project_Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_Event_Team_Team_TeamId",
                table: "Calendar_Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventHistory_Calendar_Event_EventId",
                table: "Calendar_EventHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                table: "Calendar_EventHistoryParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventHistoryParameter_Calendar_EventHistory_EventHistoryId",
                table: "Calendar_EventHistoryParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventMember_Base_UserProfile_UserProfileId",
                table: "Calendar_EventMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventMember_Calendar_Event_EventId",
                table: "Calendar_EventMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventProject_Calendar_Event_EventId",
                table: "Calendar_EventProject");

            migrationBuilder.DropForeignKey(
                name: "FK_Calendar_EventProject_Project_Project_ProjectId",
                table: "Calendar_EventProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar_EventProject",
                table: "Calendar_EventProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar_EventMember",
                table: "Calendar_EventMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar_EventHistoryParameter",
                table: "Calendar_EventHistoryParameter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar_EventHistory",
                table: "Calendar_EventHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Calendar_Event",
                table: "Calendar_Event");

            migrationBuilder.RenameTable(
                name: "Calendar_EventProject",
                newName: "Calender_EventProject");

            migrationBuilder.RenameTable(
                name: "Calendar_EventMember",
                newName: "Calender_EventMember");

            migrationBuilder.RenameTable(
                name: "Calendar_EventHistoryParameter",
                newName: "Calender_EventHistoryParameter");

            migrationBuilder.RenameTable(
                name: "Calendar_EventHistory",
                newName: "Calender_EventHistory");

            migrationBuilder.RenameTable(
                name: "Calendar_Event",
                newName: "Calender_Event");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventProject_EventId",
                table: "Calender_EventProject",
                newName: "IX_Calender_EventProject_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventMember_UserProfileId",
                table: "Calender_EventMember",
                newName: "IX_Calender_EventMember_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventMember_EventId",
                table: "Calender_EventMember",
                newName: "IX_Calender_EventMember_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventHistoryParameter_EventHistoryId",
                table: "Calender_EventHistoryParameter",
                newName: "IX_Calender_EventHistoryParameter_EventHistoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventHistoryParameter_ActivityParameterTypeId",
                table: "Calender_EventHistoryParameter",
                newName: "IX_Calender_EventHistoryParameter_ActivityParameterTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_EventHistory_EventId",
                table: "Calender_EventHistory",
                newName: "IX_Calender_EventHistory_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Calendar_Event_TeamId",
                table: "Calender_Event",
                newName: "IX_Calender_Event_TeamId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventProject",
                table: "Calender_EventProject",
                columns: new[] { "ProjectId", "EventId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventMember",
                table: "Calender_EventMember",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventHistoryParameter",
                table: "Calender_EventHistoryParameter",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_EventHistory",
                table: "Calender_EventHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Calender_Event",
                table: "Calender_Event",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_Event_Team_Team_TeamId",
                table: "Calender_Event",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventHistory_Calender_Event_EventId",
                table: "Calender_EventHistory",
                column: "EventId",
                principalTable: "Calender_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                table: "Calender_EventHistoryParameter",
                column: "ActivityParameterTypeId",
                principalTable: "ActivityParameterType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventHistoryParameter_Calender_EventHistory_EventHistoryId",
                table: "Calender_EventHistoryParameter",
                column: "EventHistoryId",
                principalTable: "Calender_EventHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventMember_Base_UserProfile_UserProfileId",
                table: "Calender_EventMember",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventMember_Calender_Event_EventId",
                table: "Calender_EventMember",
                column: "EventId",
                principalTable: "Calender_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventProject_Calender_Event_EventId",
                table: "Calender_EventProject",
                column: "EventId",
                principalTable: "Calender_Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calender_EventProject_Project_Project_ProjectId",
                table: "Calender_EventProject",
                column: "ProjectId",
                principalTable: "Project_Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
