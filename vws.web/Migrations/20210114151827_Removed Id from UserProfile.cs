using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class RemovedIdfromUserProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_DepartmentMember_Base_UserProfile_UserProfileUserId",
                table: "Department_DepartmentMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_ProjectMember_Base_UserProfile_UserProfileUserId",
                table: "Project_ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_TaskReminderLinkedUser_Base_UserProfile_RemindUserUserId",
                table: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_Team_TeamMember_Base_UserProfile_UserProfileUserId",
                table: "Team_TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_Team_TeamMember_UserProfileUserId",
                table: "Team_TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_Task_TaskReminderLinkedUser_RemindUserUserId",
                table: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropIndex(
                name: "IX_Project_ProjectMember_UserProfileUserId",
                table: "Project_ProjectMember");

            migrationBuilder.DropIndex(
                name: "IX_Department_DepartmentMember_UserProfileUserId",
                table: "Department_DepartmentMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Base_UserProfile",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "UserProfileUserId",
                table: "Team_TeamMember");

            migrationBuilder.DropColumn(
                name: "RemindUserUserId",
                table: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropColumn(
                name: "UserProfileUserId",
                table: "Project_ProjectMember");

            migrationBuilder.DropColumn(
                name: "UserProfileUserId",
                table: "Department_DepartmentMember");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Base_UserProfile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserProfileUserId",
                table: "Team_TeamMember",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemindUserUserId",
                table: "Task_TaskReminderLinkedUser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserProfileUserId",
                table: "Project_ProjectMember",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserProfileUserId",
                table: "Department_DepartmentMember",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Base_UserProfile",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Base_UserProfile",
                table: "Base_UserProfile",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamMember_UserProfileUserId",
                table: "Team_TeamMember",
                column: "UserProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskReminderLinkedUser_RemindUserUserId",
                table: "Task_TaskReminderLinkedUser",
                column: "RemindUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectMember_UserProfileUserId",
                table: "Project_ProjectMember",
                column: "UserProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentMember_UserProfileUserId",
                table: "Department_DepartmentMember",
                column: "UserProfileUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_DepartmentMember_Base_UserProfile_UserProfileUserId",
                table: "Department_DepartmentMember",
                column: "UserProfileUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_ProjectMember_Base_UserProfile_UserProfileUserId",
                table: "Project_ProjectMember",
                column: "UserProfileUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Task_TaskReminderLinkedUser_Base_UserProfile_RemindUserUserId",
                table: "Task_TaskReminderLinkedUser",
                column: "RemindUserUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Team_TeamMember_Base_UserProfile_UserProfileUserId",
                table: "Team_TeamMember",
                column: "UserProfileUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
