using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Jahhezni.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; } 

        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "الاسم يجب أن يكون بين 2 إلى 100 حرف")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [JsonIgnore] // دي بديلة لـ delete ret.password اللي كنت عاملها في Node.js لحماية الباسورد
        public required string Password { get; set; }

        [Required]
        public string Role { get; set; } = "Bride"; // bride, vendor, admin

        // حقول خاصة بالتاجر (Vendor) مع Validation يعتمد على الـ Role
        public string? ShopName { get; set; }

        public string? Category { get; set; } // furniture, electronics, etc.

        public string? Address { get; set; }

        public string? ProfileImage { get; set; }

        public bool IsVerified { get; set; } = false;

        // --- حقول إضافية عشان الـ Activity Log والـ Dashboard اللي طلبته ---

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; } // عشان تعرف مين دخل الساعة كام

        public bool IsActive { get; set; } = true; // عشان الأدمن يقدر يعمل Block لأي مستخدم
    }
}