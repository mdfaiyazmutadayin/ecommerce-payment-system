using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IPaymentOrchestrator
    {
        Task<(string transactionId, string provider, string rawResponse)> CheckoutAsync(int orderId, string provider);
        Task<bool> ConfirmPaymentAsync(string provider, string transactionId);
    }
}
