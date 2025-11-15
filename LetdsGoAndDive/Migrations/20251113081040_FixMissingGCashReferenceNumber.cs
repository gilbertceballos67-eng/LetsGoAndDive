using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetdsGoAndDive.Migrations
{
    public partial class FixMissingGCashReferenceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GCashReferenceNumber",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GCashReferenceNumber",
                table: "Orders");
        }
    }
}
