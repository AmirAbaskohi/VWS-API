using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedFileController : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_File_File_ProfileImageId",
                table: "Base_UserProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Team_Team_File_File_TeamImageId",
                table: "Team_Team");

            migrationBuilder.DropIndex(
                name: "IX_Team_Team_TeamImageId",
                table: "Team_Team");

            migrationBuilder.DropIndex(
                name: "IX_Base_UserProfile_ProfileImageId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "TeamImageId",
                table: "Team_Team");

            migrationBuilder.DropColumn(
                name: "ProfileImageId",
                table: "Base_UserProfile");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "File_File",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "FileContainerId",
                table: "File_File",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "File_FileContainer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File_FileContainer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_File_File_FileContainerId",
                table: "File_File",
                column: "FileContainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_File_File_FileContainer_FileContainerId",
                table: "File_File",
                column: "FileContainerId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_File_File_File_FileContainer_FileContainerId",
                table: "File_File");

            migrationBuilder.DropTable(
                name: "File_FileContainer");

            migrationBuilder.DropIndex(
                name: "IX_File_File_FileContainerId",
                table: "File_File");

            migrationBuilder.DropColumn(
                name: "FileContainerId",
                table: "File_File");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "File_File",
                newName: "FileId");

            migrationBuilder.AddColumn<Guid>(
                name: "TeamImageId",
                table: "Team_Team",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageId",
                table: "Base_UserProfile",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Team_Team_TeamImageId",
                table: "Team_Team",
                column: "TeamImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserProfile_ProfileImageId",
                table: "Base_UserProfile",
                column: "ProfileImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_File_File_ProfileImageId",
                table: "Base_UserProfile",
                column: "ProfileImageId",
                principalTable: "File_File",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Team_Team_File_File_TeamImageId",
                table: "Team_Team",
                column: "TeamImageId",
                principalTable: "File_File",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
