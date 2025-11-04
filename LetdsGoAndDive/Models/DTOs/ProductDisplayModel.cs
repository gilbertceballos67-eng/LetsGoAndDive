using Humanizer.Localisation;

namespace LetdsGoAndDive.Models.DTOs
{
    public class ProductDisplayModel
    {
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<ItemType> ItemTypes { get; set; }
        public string STerm { get; set; } = "";
        public int ItemTypeId { get; set; } = 0;
    }

}
