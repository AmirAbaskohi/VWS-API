using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddTeamImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamImageId",
                table: "Team_Team",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Team_Team_TeamImageId",
                table: "Team_Team",
                column: "TeamImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Team_Team_File_FileContainer_TeamImageId",
                table: "Team_Team",
                column: "TeamImageId",
                principalTable: "File_FileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Team_Team_File_FileContainer_TeamImageId",
                table: "Team_Team");

            migrationBuilder.DropIndex(
                name: "IX_Team_Team_TeamImageId",
                table: "Team_Team");

            migrationBuilder.DropColumn(
                name: "TeamImageId",
                table: "Team_Team");
        }
    }
}
