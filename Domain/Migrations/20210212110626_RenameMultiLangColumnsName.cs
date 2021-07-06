using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class RenameMultiLangColumnsName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NameMultiLang",
                table: "Team_TeamType",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NameMultiLang",
                table: "Task_TaskScheduleType",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NameMultiLang",
                table: "Task_TaskCommentTemplate",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NameMultiLang",
                table: "Project_Status",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Team_TeamType",
                newName: "NameMultiLang");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Task_TaskScheduleType",
                newName: "NameMultiLang");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Task_TaskCommentTemplate",
                newName: "NameMultiLang");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Project_Status",
                newName: "NameMultiLang");
        }
    }
}
