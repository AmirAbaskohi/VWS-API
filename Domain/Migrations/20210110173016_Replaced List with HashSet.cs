using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class ReplacedListwithHashSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Chat_Message_MessageTypeId",
                table: "Chat_Message",
                column: "MessageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chat_Message_Chat_MessageType_MessageTypeId",
                table: "Chat_Message",
                column: "MessageTypeId",
                principalTable: "Chat_MessageType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chat_Message_Chat_MessageType_MessageTypeId",
                table: "Chat_Message");

            migrationBuilder.DropIndex(
                name: "IX_Chat_Message_MessageTypeId",
                table: "Chat_Message");
        }
    }
}
