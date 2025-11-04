using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetdsGoAndDive.Models
{
    [Table("Product")]
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string? ProductName { get; set; }
        public double Price { get; set; }
        public string Image { get; set; }
        [Required]
        public int ItemTypeID { get; set; }
        public ItemType ItemType { get; set; }
        public List<OrderDetail> OrderDetail { get; set; }
        public List<CartDetail> CartDetail { get; set; }

        public Stock Stock { get; set; }
        [NotMapped]
        public int Quantity { get; set; }


        //[NotMapped]
        //public string ItemName { get; set; }
    }
}
