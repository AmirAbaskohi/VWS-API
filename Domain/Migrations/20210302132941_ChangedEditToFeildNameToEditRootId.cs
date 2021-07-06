using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class ChangedEditToFeildNameToEditRootId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EditTo",
                table: "Chat_Message",
                newName: "EditRootId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EditRootId",
                table: "Chat_Message",
                newName: "EditTo");
        }
    }
}
