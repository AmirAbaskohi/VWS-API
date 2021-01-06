using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations.TaskDb
{
    public partial class addedTaskSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.EnsureSchema(
                name: "task");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "GeneralTask",
                newSchema: "task");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralTask",
                schema: "task",
                table: "GeneralTask",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralTask",
                schema: "task",
                table: "GeneralTask");

            migrationBuilder.RenameTable(
                name: "GeneralTask",
                schema: "task",
                newName: "Messages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");
        }
    }
}
