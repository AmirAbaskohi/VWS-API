using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class AddedGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Chat_Message");

            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "Team_Team",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.NewGuid());

            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "Task_GeneralTask",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.NewGuid());

            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "Project_Project",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.NewGuid());

            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "Department_Department",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.NewGuid());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Team_Team");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Task_GeneralTask");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Project_Project");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Department_Department");

            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "Chat_Message",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
