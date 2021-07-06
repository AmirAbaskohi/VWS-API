using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class ChangeNameOfLastTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTransaction",
                table: "Channel_ChannelTransaction",
                newName: "LastTransactionDateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTransactionDateTime",
                table: "Channel_ChannelTransaction",
                newName: "LastTransaction");
        }
    }
}
