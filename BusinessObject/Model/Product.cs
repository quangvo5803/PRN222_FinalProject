using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Model
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public string? Description { get; set; }
        public double Price { get; set; }
        public int CategoryId { get; set; }
        public int? ProductAvatarId { get; set; }
        public List<ItemImage>? ProductImages { get; set; }

        //Foreign key

        [ForeignKey("ProductAvatarId")]
        public ItemImage? ProductAvatar { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        //Navigation property
        public virtual ICollection<Feedback>? Feedbacks { get; set; }
    }
}
