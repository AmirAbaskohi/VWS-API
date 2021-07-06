using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTaskClassesToDbContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Task_GeneralTask",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Task_GeneralTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedBy",
                table: "Task_GeneralTask",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Task_TaskReminder",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskReminder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TaskReminder_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskCheckLists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneralTaskId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCheckLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCheckLists_Task_GeneralTask_GeneralTaskId",
                        column: x => x.GeneralTaskId,
                        principalTable: "Task_GeneralTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskCommentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameMultiLang = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCommentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Task_TaskReminderLinkedUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskReminderId = table.Column<long>(type: "bigint", nullable: false),
                    RemindUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemindUserUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task_TaskReminderLinkedUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Task_TaskReminderLinkedUser_Base_UserProfile_RemindUserUserId",
                        column: x => x.RemindUserUserId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Task_TaskReminderLinkedUser_Task_TaskReminder_TaskReminderId",
                        column: x => x.TaskReminderId,
                        principalTable: "Task_TaskReminder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskCheckListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCheckListId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCheckListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCheckListItems_TaskCheckLists_TaskCheckListId",
                        column: x => x.TaskCheckListId,
                        principalTable: "TaskCheckLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskReminder_GeneralTaskId",
                table: "Task_TaskReminder",
                column: "GeneralTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskReminderLinkedUser_RemindUserUserId",
                table: "Task_TaskReminderLinkedUser",
                column: "RemindUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Task_TaskReminderLinkedUser_TaskReminderId",
                table: "Task_TaskReminderLinkedUser",
                column: "TaskReminderId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCheckListItems_TaskCheckListId",
                table: "TaskCheckListItems",
                column: "TaskCheckListId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCheckLists_GeneralTaskId",
                table: "TaskCheckLists",
                column: "GeneralTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Task_TaskReminderLinkedUser");

            migrationBuilder.DropTable(
                name: "TaskCheckListItems");

            migrationBuilder.DropTable(
                name: "TaskCommentTemplates");

            migrationBuilder.DropTable(
                name: "Task_TaskReminder");

            migrationBuilder.DropTable(
                name: "TaskCheckLists");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Task_GeneralTask");
        }
    }
}
