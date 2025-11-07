using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models.DTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string? ProductName { get; set; }

        [Required]
        public double Price { get; set; }

        [Required]
        public int ItemTypeID { get; set; }

        public string? Image { get; set; }
        public IFormFile? ImageFile { get; set; }

        public string? Description { get; set; }


        public IEnumerable<SelectListItem>? ItemTypeList { get; set; }
    }
}
