using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Model
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int FeedbackStars { get; set; }
        public string? FeedbackContent { get; set; }

        public Guid UserId { get; set; }

        public int ProductId { get; set; }

        //Foreign key
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public List<ItemImage>? Images { get; set; }
    }
}
