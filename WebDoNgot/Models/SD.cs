using Microsoft.AspNetCore.Mvc;

namespace WebDoNgot.Models
{
    public static class SD
    {
        public const string Role_Admin = "Admin";
        public const string Role_User = "User";

        // Order Statuses
        public const string OrderStatus_Processing = "Đang xử lý";
        public const string OrderStatus_Shipped = "Đã gửi";
        public const string OrderStatus_Delivered = "Đã giao";
        public const string OrderStatus_Cancelled = "Đã hủy";
    }
}
