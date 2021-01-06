using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations.MessageDb
{
    public partial class addChatMessageDbContextSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_MessageTypes_MessageTypeId",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageTypes",
                table: "MessageTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.RenameTable(
                name: "MessageTypes",
                newName: "MessageType",
                newSchema: "chat");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Message",
                newSchema: "chat");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_MessageTypeId",
                schema: "chat",
                table: "Message",
                newName: "IX_Message_MessageTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageType",
                schema: "chat",
                table: "MessageType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Message",
                schema: "chat",
                table: "Message",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Message_MessageType_MessageTypeId",
                schema: "chat",
                table: "Message",
                column: "MessageTypeId",
                principalSchema: "chat",
                principalTable: "MessageType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Message_MessageType_MessageTypeId",
                schema: "chat",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageType",
                schema: "chat",
                table: "MessageType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Message",
                schema: "chat",
                table: "Message");

            migrationBuilder.RenameTable(
                name: "MessageType",
                schema: "chat",
                newName: "MessageTypes");

            migrationBuilder.RenameTable(
                name: "Message",
                schema: "chat",
                newName: "Messages");

            migrationBuilder.RenameIndex(
                name: "IX_Message_MessageTypeId",
                table: "Messages",
                newName: "IX_Messages_MessageTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageTypes",
                table: "MessageTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_MessageTypes_MessageTypeId",
                table: "Messages",
                column: "MessageTypeId",
                principalTable: "MessageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
