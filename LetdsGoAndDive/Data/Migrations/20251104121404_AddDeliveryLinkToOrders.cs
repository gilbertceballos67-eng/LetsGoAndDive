using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetdsGoAndDive.Data.Migrations
{
    public partial class AddDeliveryLinkToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryLink",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryLink",
                table: "Orders");
        }
    }
}
