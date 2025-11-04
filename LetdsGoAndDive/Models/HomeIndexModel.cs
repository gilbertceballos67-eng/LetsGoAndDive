using System.Collections.Generic;
using X.PagedList;

namespace LetdsGoAndDive.Models
{
    public class HomeIndexModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<ItemType> ItemTypes { get; set; } = new List<ItemType>();
        public string STerm { get; set; } = string.Empty;
        public int ItemTypeId { get; set; }
        public int Page { get; set; } = 1;
        public IPagedList<Product> PagedProducts { get; set; }
    }
}
