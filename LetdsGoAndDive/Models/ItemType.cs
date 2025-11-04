using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{
    [Table("ItemType")]
    public class ItemType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string ItemTypeName { get; set; }
        public List<Product> Products { get; set; }
    }
}
