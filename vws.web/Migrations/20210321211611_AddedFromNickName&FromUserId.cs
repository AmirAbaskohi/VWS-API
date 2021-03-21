using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedFromNickNameFromUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromUserName",
                table: "Chat_Message");

            migrationBuilder.AddColumn<string>(
                name: "FromNickName",
                table: "Chat_Message",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromUserId",
                table: "Chat_Message",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromNickName",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "FromUserId",
                table: "Chat_Message");

            migrationBuilder.AddColumn<string>(
                name: "FromUserName",
                table: "Chat_Message",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
