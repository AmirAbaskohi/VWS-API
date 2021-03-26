using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedProfileImageSecurityStamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageSecurityStamp",
                table: "Base_UserProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE dbo.Base_UserProfile SET dbo.Base_UserProfile.ProfileImageSecurityStamp = NEWID()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageSecurityStamp",
                table: "Base_UserProfile");
        }
    }
}
