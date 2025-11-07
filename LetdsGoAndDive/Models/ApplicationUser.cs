using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string? MobileNumber { get; set; }


        [Required]
        [MaxLength(255)]
        public string Address { get; set; }
    }
}
