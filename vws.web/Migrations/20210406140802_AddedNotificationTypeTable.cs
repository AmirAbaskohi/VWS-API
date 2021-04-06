using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedNotificationTypeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Notification_Notification");

            migrationBuilder.AddColumn<long>(
                name: "ActivityId",
                table: "Notification_Notification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "NotificationTypeId",
                table: "Notification_Notification",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "Notification_NotificationType",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification_NotificationType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Notification_NotificationTypeId",
                table: "Notification_Notification",
                column: "NotificationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Notification_Notification_NotificationType_NotificationTypeId",
                table: "Notification_Notification",
                column: "NotificationTypeId",
                principalTable: "Notification_NotificationType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Notification_Notification_NotificationType_NotificationTypeId",
                table: "Notification_Notification");

            migrationBuilder.DropTable(
                name: "Notification_NotificationType");

            migrationBuilder.DropIndex(
                name: "IX_Notification_Notification_NotificationTypeId",
                table: "Notification_Notification");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "Notification_Notification");

            migrationBuilder.DropColumn(
                name: "NotificationTypeId",
                table: "Notification_Notification");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Notification_Notification",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
