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
        // Stripe calls this "create payment intent", bKash calls it "checkout" —
        // both mean "start the payment, get back a provider-side transaction id"
        Task<(string transactionId, string rawResponse)> CheckoutAsync(decimal amount);

        // Stripe: "confirm payment". bKash: "execute payment".
        Task<bool> ExecutePaymentAsync(string transactionId);

        // Stripe: retrieve PaymentIntent status. bKash: "query payment".
        Task<bool> QueryPaymentAsync(string transactionId);
    }
}
