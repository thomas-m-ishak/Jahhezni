using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class PreparationList
    {
        [Key]
        public int ListId { get; set; } // مطابق للـ ERD

        [Required]
        public int UserId { get; set; } // العروسة صاحبة القائمة

        [Required(ErrorMessage = "من فضلك حدد عنواناً للقائمة")]
        [StringLength(100)]
        public string Title { get; set; } // مثلاً "جهاز الشقة" أو "رفايع المطبخ"

        [Range(0, 10000000, ErrorMessage = "الميزانية يجب أن تكون رقماً منطقياً")]
        public decimal TotalBudget { get; set; } // الميزة اللي في الـ ERD لحساب التكاليف

        public bool Privacy { get; set; } = false; // لو حابة تشارك القائمة مع حد أو تخليها خاصة

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // ربط القائمة بالأصناف اللي جواها
        public virtual ICollection<PreparationItem> PreparationItems { get; set; } = new List<PreparationItem>();
    }
}