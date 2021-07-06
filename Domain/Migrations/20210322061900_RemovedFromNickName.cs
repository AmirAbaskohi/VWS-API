using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class RemovedFromNickName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromNickName",
                table: "Chat_Message");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromNickName",
                table: "Chat_Message",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
