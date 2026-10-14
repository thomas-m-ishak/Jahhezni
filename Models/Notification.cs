using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; } // الشخص اللي هيستلم التنبيه

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "عنوان الإشعار مطلوب")]
        public string Title { get; set; }

        [Required(ErrorMessage = "محتوى الإشعار مطلوب")]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}