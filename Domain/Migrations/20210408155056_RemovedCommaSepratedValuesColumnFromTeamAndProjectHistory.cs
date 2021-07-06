using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class RemovedCommaSepratedValuesColumnFromTeamAndProjectHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommaSepratedParameters",
                table: "Team_TeamHistory");

            migrationBuilder.DropColumn(
                name: "CommaSepratedParameters",
                table: "Project_ProjectHistory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommaSepratedParameters",
                table: "Team_TeamHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommaSepratedParameters",
                table: "Project_ProjectHistory",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
