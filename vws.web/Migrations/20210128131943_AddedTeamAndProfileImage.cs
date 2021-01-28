using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTeamAndProfileImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isDeleted",
                table: "Team_Team",
                newName: "IsDeleted");

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

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "IsDeleted",
                table: "Team_Team",
                newName: "isDeleted");
        }
    }
}
