using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedAddedTeamAndProjectHistoryParamTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Project_ProjectHistoryParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityParameterTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    ProjectHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project_ProjectHistoryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_ProjectHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                        column: x => x.ActivityParameterTypeId,
                        principalTable: "ActivityParameterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_ProjectHistoryParameter_Project_ProjectHistory_ProjectHistoryId",
                        column: x => x.ProjectHistoryId,
                        principalTable: "Project_ProjectHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Team_TeamHistoryParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityParameterTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    TeamHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team_TeamHistoryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Team_TeamHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                        column: x => x.ActivityParameterTypeId,
                        principalTable: "ActivityParameterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Team_TeamHistoryParameter_Team_TeamHistory_TeamHistoryId",
                        column: x => x.TeamHistoryId,
                        principalTable: "Team_TeamHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectHistoryParameter_ActivityParameterTypeId",
                table: "Project_ProjectHistoryParameter",
                column: "ActivityParameterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectHistoryParameter_ProjectHistoryId",
                table: "Project_ProjectHistoryParameter",
                column: "ProjectHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamHistoryParameter_ActivityParameterTypeId",
                table: "Team_TeamHistoryParameter",
                column: "ActivityParameterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_TeamHistoryParameter_TeamHistoryId",
                table: "Team_TeamHistoryParameter",
                column: "TeamHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Project_ProjectHistoryParameter");

            migrationBuilder.DropTable(
                name: "Team_TeamHistoryParameter");
        }
    }
}
