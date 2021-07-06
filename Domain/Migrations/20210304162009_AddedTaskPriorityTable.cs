using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskPriorityTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "Task_GeneralTask",
                newName: "PriorityId");

            migrationBuilder.AddColumn<byte>(
                name: "TaskPriorityId",
                table: "Task_GeneralTask",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.CreateTable(
                name: "Task_TaskPriority",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskPriority", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_GeneralTask_TaskPriorityId",
                table: "Task_GeneralTask",
                column: "TaskPriorityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask",
                column: "TaskPriorityId",
                principalTable: "Task_TaskPriority",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_GeneralTask_Task_TaskPriority_TaskPriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.DropTable(
                name: "Task_TaskPriority");

            migrationBuilder.DropIndex(
                name: "IX_Task_GeneralTask_TaskPriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "TaskPriorityId",
                table: "Task_GeneralTask");

            migrationBuilder.RenameColumn(
                name: "PriorityId",
                table: "Task_GeneralTask",
                newName: "Priority");
        }
    }
}
