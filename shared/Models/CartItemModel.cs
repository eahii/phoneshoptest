namespace Shared.Models
{

    public class CartItemModel
    {
        public int CartItemID { get; set; }
        public int Quantity { get; set; }
        public int PhoneID { get; set; }
        public PhoneModel Phone { get; set; } // <-- Add this property
    }

    public class CartItemWithPhone
    {
        public int CartItemID { get; set; }
        public int CartID { get; set; }
        public int Quantity { get; set; }
        public PhoneModel Phone { get; set; }
    }
}
