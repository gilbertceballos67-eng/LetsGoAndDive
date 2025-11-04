using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models.DTOs
{
    public class ItemTypeDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item Type Name is required")]
        [MaxLength(40, ErrorMessage = "Item Type Name cannot exceed 40 characters")]
        public string ItemTypeName { get; set; } = string.Empty;
    }
}
