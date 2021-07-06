using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedGuidForProfileImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamImageGuid",
                table: "Team_Team",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectImageGuid",
                table: "Project_Project",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentImageGuid",
                table: "Department_Department",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageGuid",
                table: "Base_UserProfile",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamImageGuid",
                table: "Team_Team");

            migrationBuilder.DropColumn(
                name: "ProjectImageGuid",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "DepartmentImageGuid",
                table: "Department_Department");

            migrationBuilder.DropColumn(
                name: "ProfileImageGuid",
                table: "Base_UserProfile");
        }
    }
}
