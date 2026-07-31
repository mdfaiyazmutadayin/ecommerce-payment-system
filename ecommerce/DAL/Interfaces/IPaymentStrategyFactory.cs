using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IPaymentStrategyFactory
    {
        IPaymentStrategy GetStrategy(string provider);
    }

    public interface IPaymentStrategy
    {
        Task<(string transactionId, string rawResponse)> CheckoutAsync(decimal amount);
        Task<bool> ExecutePaymentAsync(string transactionId);
        Task<bool> QueryPaymentAsync(string transactionId);
    }
}
