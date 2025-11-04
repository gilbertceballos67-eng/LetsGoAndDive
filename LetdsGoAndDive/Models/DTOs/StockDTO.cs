using System.ComponentModel.DataAnnotations;
namespace LetdsGoAndDive.Models.DTOs
{
    public class StockDTO
    {
        public int ProductId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative value.")]
        public int Quantity { get; set; }
    }
}
