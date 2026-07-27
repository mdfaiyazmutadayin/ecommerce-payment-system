using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Order : Base
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // FIXED: Renamed 'Items' to 'OrderItems' so it matches your services and orchestrator.
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // DELETED: public object OrderItems { get; internal set; }
    }
}