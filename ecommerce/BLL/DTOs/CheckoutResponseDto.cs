using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CheckoutResponseDto
    {
        public string Provider { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string RedirectUrl { get; set; } // bKash's hosted payment page; null for Stripe
    }
}
