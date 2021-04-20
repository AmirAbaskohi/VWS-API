using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedFeedBackTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedBack_FeedBack",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Descriptiom = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentId = table.Column<int>(type: "int", nullable: true),
                    AttachmentGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedBack_FeedBack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedBack_FeedBack_Base_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Base_UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedBack_FeedBack_File_FileContainer_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "File_FileContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedBack_FeedBack_AttachmentId",
                table: "FeedBack_FeedBack",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedBack_FeedBack_UserProfileId",
                table: "FeedBack_FeedBack",
                column: "UserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedBack_FeedBack");
        }
    }
}
