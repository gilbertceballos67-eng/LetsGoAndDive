namespace LetdsGoAndDive.Models.DTOs
{
    public class SalesByProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
    }
}
