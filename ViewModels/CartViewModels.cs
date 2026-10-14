namespace Jahhezni.ViewModels
{
    public class UpdateQtyRequest
    {
        public int ItemId { get; set; }
        public int Change { get; set; }
    }

    public class RemoveItemRequest
    {
        public int ItemId { get; set; }
    }
}