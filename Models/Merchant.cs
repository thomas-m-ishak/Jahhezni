using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Merchant
    {
        [Key]
        public int MerchantId { get; set; } // مطابق للـ ERD

        [Required]
        public int UserId { get; set; } // الربط بجدول المستخدمين الأساسي

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "اسم المحل مطلوب")]
        [StringLength(150, ErrorMessage = "اسم المحل طويل جداً")]
        public string StoreName { get; set; } // مطابق لـ StoreName في الـ ERD

        [Required(ErrorMessage = "القسم مطلوب")]
        public string Category { get; set; } // furniture, electronics, etc.

        public string? Description { get; set; } // بديل لـ Bio في Node.js مطابق للـ ERD

        [Range(0, 5)]
        public double Rating { get; set; } = 0;

        // روابط التواصل الاجتماعي
        public string? FacebookLink { get; set; }
        public string? InstagramLink { get; set; }
        public string? WhatsappNumber { get; set; }

        // --- حقول لزوم الـ Dashboard والـ Activity Log ---

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsStoreActive { get; set; } = true; // عشان الأدمن يقدر يوقف محل معين

        // ربط التاجر بمنتجاته (علاقة 1-to-Many)
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}