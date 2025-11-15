using System.Collections.Generic;

namespace LetdsGoAndDive.Models.DTOs
{
    public class InventoryDashboardViewModel
    {
        public List<StockDisplayModel> StockSummary { get; set; } = new();
        public List<SalesByProductDto> SalesByProduct { get; set; } = new();
    }
}
