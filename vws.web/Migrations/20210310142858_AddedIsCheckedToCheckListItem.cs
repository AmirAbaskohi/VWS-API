using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedIsCheckedToCheckListItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChecked",
                table: "Task_TaskCheckListItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChecked",
                table: "Task_TaskCheckListItem");
        }
    }
}
