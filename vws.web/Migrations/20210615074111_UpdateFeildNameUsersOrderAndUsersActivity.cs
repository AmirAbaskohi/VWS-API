using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdateFeildNameUsersOrderAndUsersActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_UserId",
                table: "Base_UsersActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_UserProfileId",
                table: "Base_UsersActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_UserId",
                table: "Base_UsersOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_UserProfileId",
                table: "Base_UsersOrder");

            migrationBuilder.RenameColumn(
                name: "UserProfileId",
                table: "Base_UsersOrder",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Base_UsersOrder",
                newName: "TargetUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersOrder_UserProfileId",
                table: "Base_UsersOrder",
                newName: "IX_Base_UsersOrder_OwnerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersOrder_UserId",
                table: "Base_UsersOrder",
                newName: "IX_Base_UsersOrder_TargetUserId");

            migrationBuilder.RenameColumn(
                name: "UserProfileId",
                table: "Base_UsersActivity",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Base_UsersActivity",
                newName: "TargetUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersActivity_UserProfileId",
                table: "Base_UsersActivity",
                newName: "IX_Base_UsersActivity_OwnerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersActivity_UserId",
                table: "Base_UsersActivity",
                newName: "IX_Base_UsersActivity_TargetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_OwnerUserId",
                table: "Base_UsersActivity",
                column: "OwnerUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_TargetUserId",
                table: "Base_UsersActivity",
                column: "TargetUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_OwnerUserId",
                table: "Base_UsersOrder",
                column: "OwnerUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_TargetUserId",
                table: "Base_UsersOrder",
                column: "TargetUserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_OwnerUserId",
                table: "Base_UsersActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_TargetUserId",
                table: "Base_UsersActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_OwnerUserId",
                table: "Base_UsersOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_TargetUserId",
                table: "Base_UsersOrder");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Base_UsersOrder",
                newName: "UserProfileId");

            migrationBuilder.RenameColumn(
                name: "TargetUserId",
                table: "Base_UsersOrder",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersOrder_OwnerUserId",
                table: "Base_UsersOrder",
                newName: "IX_Base_UsersOrder_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersOrder_TargetUserId",
                table: "Base_UsersOrder",
                newName: "IX_Base_UsersOrder_UserId");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Base_UsersActivity",
                newName: "UserProfileId");

            migrationBuilder.RenameColumn(
                name: "TargetUserId",
                table: "Base_UsersActivity",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersActivity_OwnerUserId",
                table: "Base_UsersActivity",
                newName: "IX_Base_UsersActivity_UserProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Base_UsersActivity_TargetUserId",
                table: "Base_UsersActivity",
                newName: "IX_Base_UsersActivity_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_UserId",
                table: "Base_UsersActivity",
                column: "UserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersActivity_Base_UserProfile_UserProfileId",
                table: "Base_UsersActivity",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_UserId",
                table: "Base_UsersOrder",
                column: "UserId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Base_UsersOrder_Base_UserProfile_UserProfileId",
                table: "Base_UsersOrder",
                column: "UserProfileId",
                principalTable: "Base_UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);
        }
    }
}
