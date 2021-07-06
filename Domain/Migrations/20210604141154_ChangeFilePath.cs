using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class ChangeFilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE dbo.File_File
SET
	Address = SUBSTRING(Address, CHARINDEX('Upload', Address), LEN(Address) - CHARINDEX('Upload', Address) + 1)
                                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
