﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Model
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public double TotalPrice { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "PayByCash" hoặc "VNPay"

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string? ShippingAddress { get; set; }
        public string? PhoneNumber { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Completed,
        Cancelled,
    }
}
