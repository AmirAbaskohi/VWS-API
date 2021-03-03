using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedEditMessageTableAndUpdatedMessageTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditRootId",
                table: "Chat_Message");

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Chat_Message",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Chat_MessageEdit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OldBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NewBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat_MessageEdit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chat_MessageEdit_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chat_MessageEdit_Chat_Message_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Chat_Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chat_MessageEdit_MessageId",
                table: "Chat_MessageEdit",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Chat_MessageEdit_UserProfileId",
                table: "Chat_MessageEdit",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chat_MessageEdit");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Chat_Message");

            migrationBuilder.AddColumn<long>(
                name: "EditRootId",
                table: "Chat_Message",
                type: "bigint",
                nullable: true);
        }
    }
}
