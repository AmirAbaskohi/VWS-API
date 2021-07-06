using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedUsersOrderAndUsersActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Base_UsersActivity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Base_UsersActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Base_UsersActivity_Base_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Base_UsersActivity_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Base_UsersOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Base_UsersOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Base_UsersOrder_Base_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Base_UsersOrder_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Base_UsersActivity_UserId",
                table: "Base_UsersActivity",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UsersActivity_UserProfileId",
                table: "Base_UsersActivity",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UsersOrder_UserId",
                table: "Base_UsersOrder",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Base_UsersOrder_UserProfileId",
                table: "Base_UsersOrder",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Base_UsersActivity");

            migrationBuilder.DropTable(
                name: "Base_UsersOrder");
        }
    }
}
