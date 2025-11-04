using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{
    [Table("ShoppingCart")]
    public class Shoppingcart
    {
        public int Id { get; set; }
        [Required]

        public string UserId { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<CartDetail> CartDetails { get; set; }
    }
}
