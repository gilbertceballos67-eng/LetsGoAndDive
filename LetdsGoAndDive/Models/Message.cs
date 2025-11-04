using System;
using System.ComponentModel.DataAnnotations;

namespace LetdsGoAndDive.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Sender { get; set; }

        [Required]
        public string Receiver { get; set; }

        [Required]
        public string Text { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
