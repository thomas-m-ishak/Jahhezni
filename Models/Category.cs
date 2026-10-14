using System.ComponentModel.DataAnnotations;

namespace Jahhezni.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        public string Name { get; set; } // furniture, electronics, etc.

        public string? ImageUrl { get; set; } // أيقونة للقسم

        public bool IsActive { get; set; } = true;
    }
}