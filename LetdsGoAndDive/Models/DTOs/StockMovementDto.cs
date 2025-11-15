namespace LetdsGoAndDive.Models.DTOs
{
    public class StockMovementDto
    {
        public DateTime Date { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
    }
}
