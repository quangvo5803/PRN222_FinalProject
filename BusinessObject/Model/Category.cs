using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Model
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }
    }
}
