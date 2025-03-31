using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Model
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid SenderId { get; set; } 

        public Guid? AdminId { get; set; }

        [Required]
        public required string Content { get; set; }

        public bool IsRead { get; set; } = false; 

        public bool IsResolved { get; set; } = false; 

        public DateTime SentAt { get; set; } = DateTime.Now;


        [ForeignKey("SenderId")]
        public User? Sender { get; set; }

        [ForeignKey("AdminId")]
        public User? Admin { get; set; }
    }
}
