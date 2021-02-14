using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedProjectDepartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_Project_Department_Department_DepartmentId",
                table: "Project_Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_Project_DepartmentId",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Project_Project");

            migrationBuilder.CreateTable(
                name: "Project_ProjectDepartment",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project_ProjectDepartment", x => new { x.ProjectId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_Project_ProjectDepartment_Department_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department_Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_ProjectDepartment_Project_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project_Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectDepartment_DepartmentId",
                table: "Project_ProjectDepartment",
                column: "DepartmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Project_ProjectDepartment");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Project_Project",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_Project_DepartmentId",
                table: "Project_Project",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Project_Department_Department_DepartmentId",
                table: "Project_Project",
                column: "DepartmentId",
                principalTable: "Department_Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
