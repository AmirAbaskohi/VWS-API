using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedImageToProjectAndDepartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectImageId",
                table: "Project_Project",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentImageId",
                table: "Department_Department",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_Project_ProjectImageId",
                table: "Project_Project",
                column: "ProjectImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Department_DepartmentImageId",
                table: "Department_Department",
                column: "DepartmentImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Department_File_FileContainer_DepartmentImageId",
                table: "Department_Department",
                column: "DepartmentImageId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Project_File_FileContainer_ProjectImageId",
                table: "Project_Project",
                column: "ProjectImageId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_Department_File_FileContainer_DepartmentImageId",
                table: "Department_Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Project_File_FileContainer_ProjectImageId",
                table: "Project_Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_Project_ProjectImageId",
                table: "Project_Project");

            migrationBuilder.DropIndex(
                name: "IX_Department_Department_DepartmentImageId",
                table: "Department_Department");

            migrationBuilder.DropColumn(
                name: "ProjectImageId",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "DepartmentImageId",
                table: "Department_Department");
        }
    }
}
