using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey("VendorId")]
        public virtual User? Vendor { get; set; } // أضفنا ? لمنع مشاكل الـ Validation في الـ Create

        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        [StringLength(200, ErrorMessage = "اسم المنتج طويل جداً")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "وصف المنتج مطلوب")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.1, 1000000, ErrorMessage = "السعر يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18, 2)")] // تحديد الدقة في SQL لعدم حدوث مشاكل في التحويل
        public decimal Price { get; set; }

        [Required(ErrorMessage = "القسم مطلوب")]
        public string Category { get; set; } = string.Empty;

        // جعلنا الحقل يقبل Null عشان ما يضربش Error لو فيه منتج قديم صورته ناقصة
        public string? Images { get; set; } = "";

        [Required(ErrorMessage = "الكمية المتاحة مطلوبة")]
        [Range(0, 10000, ErrorMessage = "الكمية لا يمكن أن تكون بالسالب")]
        public int Stock { get; set; } = 1;

        [Range(0, 5)]
        public double Rating { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now; // استخدمت Now المحلي لسهولة قراءة الوقت في مصر

        public bool IsActive { get; set; } = true;

        public int ViewCount { get; set; } = 0;

    }
}