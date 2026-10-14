using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; } // المعرف الخاص بالعروسة (Bride)

        [Required]
        public int MerchantId { get; set; } // المعرف الخاص بالتاجر (Vendor)

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant Merchant { get; set; }

        [Required(ErrorMessage = "نوع الخدمة مطلوب")]
        public string ServiceType { get; set; } // makeup, photography, hall, etc.

        [Required(ErrorMessage = "تاريخ الحجز مطلوب")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } // تاريخ اليوم المحجوز

        public string? Time { get; set; } // الساعة (مثلاً 06:00 PM)

        [Range(0.1, 1000000, ErrorMessage = "السعر يجب أن يكون قيمة موجبة")]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = "pending"; // pending, confirmed, completed, cancelled

        [StringLength(500, ErrorMessage = "الملاحظات لا يجب أن تتخطى 500 حرف")]
        public string? Notes { get; set; }

        // --- حقول لزوم الـ Dashboard والـ Activity Log ---

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // حقل إضافي للأدمن: هل تم الدفع عربون (Deposit)؟
        public bool IsDepositPaid { get; set; } = false;
    }
}