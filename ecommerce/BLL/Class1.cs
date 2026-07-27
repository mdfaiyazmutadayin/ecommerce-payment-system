using DAL.Models;
using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    // Contracts (previously missing — this is why you saw
    // "type or namespace could not be found" errors)

    public interface IOrderService
    {
        Task<int> CreateOrderAsync(int userId, CreateOrderDto dto);
    }

    public interface IStockService
    {
        Task ValidateAndReduceAsync(IEnumerable<OrderItem> items);
    }

    public interface IPaymentOrchestrator
    {
        Task<(string transactionId, string provider, string rawResponse)> CheckoutAsync(int orderId, string provider);
        Task<bool> ConfirmPaymentAsync(string provider, string transactionId);
    }

    public interface IPaymentStrategy
    {
        Task<(string transactionId, string rawResponse)> InitiateAsync(Order order);
        Task<bool> VerifyAsync(string transactionId);
    }

    public interface IPaymentStrategyFactory
    {
        IPaymentStrategy Resolve(string provider);
    }

    // NOTE: No concrete Stripe/bKash strategy classes exist yet in this
    // project, so Resolve() has nothing real to return. This makes the
    // solution compile, but Checkout/ConfirmPayment will throw at runtime
    // until you plug in actual provider implementations.
    public class DefaultPaymentStrategyFactory : IPaymentStrategyFactory
    {
        public IPaymentStrategy Resolve(string provider)
        {
            throw new NotSupportedException(
                $"No payment strategy implemented yet for provider '{provider}'.");
        }
    }
}