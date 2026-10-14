using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jahhezni.Models
{
    public class PreparationItem
    {
        [Key]
        public int ItemId { get; set; } // مطابق للـ ERD

        [Required]
        public int ListId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, 100)]
        public int DesiredQty { get; set; } = 1; // الكمية اللي العروسة محتاجاها

        public decimal EstimatedPrice { get; set; } // السعر التقديري للمقارنة

        public int PurchasedQty { get; set; } = 0; // عشان الـ Dashboard توريها هي خلصت قد إيه من جهازها

        [ForeignKey("ListId")]
        public virtual PreparationList PreparationList { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}