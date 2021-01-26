using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedIdtoRefreshToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Base_RefreshToken",
                table: "Base_RefreshToken");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "Base_RefreshToken",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Base_RefreshToken",
                table: "Base_RefreshToken",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Base_RefreshToken",
                table: "Base_RefreshToken");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Base_RefreshToken");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Base_RefreshToken",
                table: "Base_RefreshToken",
                column: "UserId");
        }
    }
}
