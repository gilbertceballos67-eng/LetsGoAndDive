using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetdsGoAndDive.Data.Migrations
{
    public partial class FixProductId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shoppingcart_Id",
                table: "CartDetail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Shoppingcart_Id",
                table: "CartDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
