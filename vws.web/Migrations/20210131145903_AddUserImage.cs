using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddUserImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfileImageId",
                table: "Base_UserProfile",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Base_UserProfile_ProfileImageId",
                table: "Base_UserProfile",
                column: "ProfileImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_File_FileContainer_ProfileImageId",
                table: "Base_UserProfile",
                column: "ProfileImageId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_File_FileContainer_ProfileImageId",
                table: "Base_UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_Base_UserProfile_ProfileImageId",
                table: "Base_UserProfile");

            migrationBuilder.DropColumn(
                name: "ProfileImageId",
                table: "Base_UserProfile");
        }
    }
}
