using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedChannelTypecs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "Chat_Message",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "ChannelTypeId",
                table: "Chat_Message",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "FromUserName",
                table: "Chat_Message",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplyTo",
                table: "Chat_Message",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "SendOn",
                table: "Chat_Message",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Chat_ChannelType",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat_ChannelType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chat_Message_ChannelTypeId",
                table: "Chat_Message",
                column: "ChannelTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chat_Message_Chat_ChannelType_ChannelTypeId",
                table: "Chat_Message",
                column: "ChannelTypeId",
                principalTable: "Chat_ChannelType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chat_Message_Chat_ChannelType_ChannelTypeId",
                table: "Chat_Message");

            migrationBuilder.DropTable(
                name: "Chat_ChannelType");

            migrationBuilder.DropIndex(
                name: "IX_Chat_Message_ChannelTypeId",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "ChannelTypeId",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "FromUserName",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Chat_Message");

            migrationBuilder.DropColumn(
                name: "SendOn",
                table: "Chat_Message");
        }
    }
}
