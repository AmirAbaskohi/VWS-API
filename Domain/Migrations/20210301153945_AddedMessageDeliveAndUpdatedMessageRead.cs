using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedMessageDeliveAndUpdatedMessageRead : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhoReadUserName",
                table: "Chat_MessageRead");

            migrationBuilder.AddColumn<Guid>(
                name: "ChannelId",
                table: "Chat_MessageRead",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte>(
                name: "ChannelTypeId",
                table: "Chat_MessageRead",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReadBy",
                table: "Chat_MessageRead",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Chat_MessageDeliver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    ReadBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelTypeId = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat_MessageDeliver", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chat_MessageDeliver_Chat_Message_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Chat_Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chat_MessageDeliver_MessageId",
                table: "Chat_MessageDeliver",
                column: "MessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chat_MessageDeliver");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Chat_MessageRead");

            migrationBuilder.DropColumn(
                name: "ChannelTypeId",
                table: "Chat_MessageRead");

            migrationBuilder.DropColumn(
                name: "ReadBy",
                table: "Chat_MessageRead");

            migrationBuilder.AddColumn<string>(
                name: "WhoReadUserName",
                table: "Chat_MessageRead",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
