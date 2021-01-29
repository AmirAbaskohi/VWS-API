using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedVirtualOfTeamInLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamInviteLink_TeamId",
                table: "Team_TeamInviteLink",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Team_TeamInviteLink_Team_Team_TeamId",
                table: "Team_TeamInviteLink",
                column: "TeamId",
                principalTable: "Team_Team",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Team_TeamInviteLink_Team_Team_TeamId",
                table: "Team_TeamInviteLink");

            migrationBuilder.DropIndex(
                name: "IX_Team_TeamInviteLink_TeamId",
                table: "Team_TeamInviteLink");
        }
    }
}
