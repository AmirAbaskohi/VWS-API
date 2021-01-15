using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class nullableCultureId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_Base_Culture_CultureId",
                table: "Base_UserProfile");

            migrationBuilder.AlterColumn<byte>(
                name: "CultureId",
                table: "Base_UserProfile",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_Base_Culture_CultureId",
                table: "Base_UserProfile",
                column: "CultureId",
                principalTable: "Base_Culture",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UserProfile_Base_Culture_CultureId",
                table: "Base_UserProfile");

            migrationBuilder.AlterColumn<byte>(
                name: "CultureId",
                table: "Base_UserProfile",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UserProfile_Base_Culture_CultureId",
                table: "Base_UserProfile",
                column: "CultureId",
                principalTable: "Base_Culture",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
