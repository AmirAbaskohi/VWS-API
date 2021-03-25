using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedTeamHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Team_TeamHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommaSepratedParameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team_TeamHistory", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Team_TeamHistory");
        }
    }
}
