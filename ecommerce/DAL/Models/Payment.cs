using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Payment : Base
    {
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string Provider { get; set; } = string.Empty;      // stripe | bkash
        public string TransactionId { get; set; } = string.Empty; // unique
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string RawResponse { get; set; } = "{}";
    }
}
