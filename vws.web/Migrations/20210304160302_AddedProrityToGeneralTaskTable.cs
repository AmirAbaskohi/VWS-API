using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedProrityToGeneralTaskTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Priority",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Task_GeneralTask");
        }
    }
}
