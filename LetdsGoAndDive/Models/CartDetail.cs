using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{
    [Table("CartDetail")]
    public class CartDetail
    {
        public int Id { get; set; }
        [Required]
        public int ShoppingcartId { get; set; }
        [Required]
        public Shoppingcart Shoppingcart { get; set; }

        public int ProductId { get; set; }   
        public Product Product { get; set; }
        [Required]

        public double UnitPrice { get; set; }
        [Required]
        public int Quanntity { get; set; }
    }

}
