using System.ComponentModel.DataAnnotations;

namespace Jahhezni.Models
{
    public class ActivityLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; } // مين اللي عمل الحركة (ممكن يكون null لو زائر)

        [Required]
        public string Action { get; set; } // مثلاً: Login, Add Product, Delete User

        [Required]
        public string EntityName { get; set; } // اسم الجدول اللي اتأثر (Products, Orders)

        public string? Details { get; set; } // تفاصيل إضافية (مثلاً: مسح المنتج رقم 5)

        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? DeviceInfo { get; set; } // دخل من موبايل ولا ويب
    }
}