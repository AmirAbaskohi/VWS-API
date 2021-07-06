using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTeamAndDepartmentToProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Project_Project",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Project_Project",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_Project_DepartmentId",
                table: "Project_Project",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Project_TeamId",
                table: "Project_Project",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Project_Department_Department_DepartmentId",
                table: "Project_Project",
                column: "DepartmentId",
                principalTable: "Department_Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Project_Team_Team_TeamId",
                table: "Project_Project",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_Project_Department_Department_DepartmentId",
                table: "Project_Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Project_Team_Team_TeamId",
                table: "Project_Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_Project_DepartmentId",
                table: "Project_Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_Project_TeamId",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Project_Project");
        }
    }
}
