using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{

    [Table("Orders")]
    public class Order
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        [Required]
        public int OrderStatusId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        [MaxLength(30)]
        public string? Name { get; set; }

       
        [EmailAddress]
        [MaxLength(30)]
        public string? Email { get; set; }
        
        public string? MobileNumber { get; set; }
       
        [MaxLength(200)]
        public string? Address { get; set; }
       
        [MaxLength(30)]
        public string? PaymentMethod { get; set; }
        public bool IsPaid { get; set; }

        public OrderStatus OrderStatus { get; set; }
        public List<OrderDetail> OrderDetail { get; set; }

        public string? ProofOfPaymentImagePath { get; set; }

        public string? DeliveryLink { get; set; }


    }
}
