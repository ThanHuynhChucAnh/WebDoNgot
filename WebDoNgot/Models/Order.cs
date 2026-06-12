using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebDoNgot.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal ShippingFee { get; set; } = 15000;
        public decimal Discount { get; set; } = 0;
        public string ShippingAddress { get; set; }
        public string Notes { get; set; }

        // Trạng thái đơn hàng
        [Required]
        public string Status { get; set; } = SD.OrderStatus_Processing;

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser ApplicationUser { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}