using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        [Required]
        public decimal Amount { get; set; }

        // Paymob أو Cash
        [Required]
        public string PaymentMethod { get; set; }

        // Pending / Paid / Failed
        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        // رقم العملية من Paymob
        public string? TransactionId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}