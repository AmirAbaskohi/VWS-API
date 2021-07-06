using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedHistoryTablesForDepartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Department_DepartmentHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    EventBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department_DepartmentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_DepartmentHistory_Department_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department_Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Department_DepartmentHistoryParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityParameterTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    DepartmentHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShouldBeLocalized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department_DepartmentHistoryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_DepartmentHistoryParameter_ActivityParameterType_ActivityParameterTypeId",
                        column: x => x.ActivityParameterTypeId,
                        principalTable: "ActivityParameterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Department_DepartmentHistoryParameter_Department_DepartmentHistory_DepartmentHistoryId",
                        column: x => x.DepartmentHistoryId,
                        principalTable: "Department_DepartmentHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentHistory_DepartmentId",
                table: "Department_DepartmentHistory",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentHistoryParameter_ActivityParameterTypeId",
                table: "Department_DepartmentHistoryParameter",
                column: "ActivityParameterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentHistoryParameter_DepartmentHistoryId",
                table: "Department_DepartmentHistoryParameter",
                column: "DepartmentHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Department_DepartmentHistoryParameter");

            migrationBuilder.DropTable(
                name: "Department_DepartmentHistory");
        }
    }
}
