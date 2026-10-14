namespace Jahhezni.ViewModels
{
    public class PaymentVM
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}