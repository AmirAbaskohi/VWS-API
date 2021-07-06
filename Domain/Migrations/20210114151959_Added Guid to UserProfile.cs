using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedGuidtoUserProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Base_UserProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Base_UserProfile",
                table: "Base_UserProfile",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamMember_UserProfileId",
                table: "Team_TeamMember",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskReminderLinkedUser_RemindUserId",
                table: "Task_TaskReminderLinkedUser",
                column: "RemindUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectMember_UserProfileId",
                table: "Project_ProjectMember",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentMember_UserProfileId",
                table: "Department_DepartmentMember",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_DepartmentMember_Base_UserProfile_UserProfileId",
                table: "Department_DepartmentMember",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_ProjectMember_Base_UserProfile_UserProfileId",
                table: "Project_ProjectMember",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskReminderLinkedUser_Base_UserProfile_RemindUserId",
                table: "Task_TaskReminderLinkedUser",
                column: "RemindUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Team_TeamMember_Base_UserProfile_UserProfileId",
                table: "Team_TeamMember",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_DepartmentMember_Base_UserProfile_UserProfileId",
                table: "Department_DepartmentMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_ProjectMember_Base_UserProfile_UserProfileId",
                table: "Project_ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskReminderLinkedUser_Base_UserProfile_RemindUserId",
                table: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_Team_TeamMember_Base_UserProfile_UserProfileId",
                table: "Team_TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_Team_TeamMember_UserProfileId",
                table: "Team_TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_Task_TaskReminderLinkedUser_RemindUserId",
                table: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropIndex(
                name: "IX_Project_ProjectMember_UserProfileId",
                table: "Project_ProjectMember");

            migrationBuilder.DropIndex(
                name: "IX_Department_DepartmentMember_UserProfileId",
                table: "Department_DepartmentMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Base_UserProfile",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Base_UserProfile");
        }
    }
}
