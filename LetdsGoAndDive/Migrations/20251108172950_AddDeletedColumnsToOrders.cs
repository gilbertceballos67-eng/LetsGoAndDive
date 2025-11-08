using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetdsGoAndDive.Migrations
{
    public partial class AddDeletedColumnsToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Orders");
        }
    }
}
