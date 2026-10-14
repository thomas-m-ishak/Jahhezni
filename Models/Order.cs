using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; } // مطابق للـ ERD

        [Required]
        public int UserId { get; set; } // المعرف الخاص بالعروسة (Bride)

        [Required]
        public int MerchantId { get; set; } // المعرف الخاص بالتاجر

        [Required(ErrorMessage = "إجمالي السعر مطلوب")]
        [Range(0.1, 1000000)]
        public decimal OrderTotal { get; set; } // استخدام decimal للدقة المالية

        [Required]
        public string Status { get; set; } = "pending"; // pending, confirmed, delivered, cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاريخ الطلب

        // العلاقات (Navigation Properties) لربط الجداول ببعضها
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}