using AutoMapper;
using BLL.DTOs;
using DAL.Enums;
using DAL.Interfaces;
using DAL.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class PaymentOrchestrator : IPaymentOrchestrator
    {
        private readonly IOrderRepo _orderRepo;
        private readonly IPaymentRepo _paymentRepo;
        private readonly StockService _stockService;
        private readonly DAL.Interfaces.IPaymentStrategyFactory _strategyFactory;

        public PaymentOrchestrator(
            IOrderRepo orderRepo,
            IPaymentRepo paymentRepo,
            StockService stockService,
            DAL.Interfaces.IPaymentStrategyFactory strategyFactory)
        {
            _orderRepo = orderRepo;
            _paymentRepo = paymentRepo;
            _stockService = stockService;
            _strategyFactory = strategyFactory;
        }

        public async Task<(string transactionId, string provider, string rawResponse)> CheckoutAsync(int orderId, string provider)
        {
            var order = await _orderRepo.GetOrderWithItemsAsync(orderId)
                        ?? throw new Exception("Order not found");

            var strategy = _strategyFactory.GetStrategy(provider);
            var (transactionId, rawResponse) = await strategy.CheckoutAsync(order.TotalAmount);

            // Store the payment record — transactionId here is session.Id (cs_xxx) for Stripe
            _paymentRepo.Create(new Payment
            {
                OrderId = orderId,
                Provider = provider.ToLower(),
                TransactionId = transactionId,   // cs_xxx for Stripe, paymentID for bKash
                RawResponse = rawResponse,
                Status = PaymentStatus.Pending
            });
            await _paymentRepo.SaveChangesAsync();

            return (transactionId, provider, rawResponse);
        }

        public async Task<string> GetPaymentStatusAsync(string provider, string transactionId)
        {
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(transactionId, provider.ToLower());
            if (payment == null) return "not_found";
            return payment.Status.ToString().ToLower();
        }

        public async Task MarkFailedAsync(string provider, string transactionId)
        {
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(transactionId, provider.ToLower());
            if (payment == null) return;

            payment.Status = PaymentStatus.Failed;
            await _paymentRepo.SaveChangesAsync();
        }

        public async Task<bool> ConfirmByProviderTransactionAsync(string provider, string providerTransactionId)
        {
            // providerTransactionId = session.Id (cs_xxx) for Stripe — matches what we stored in CheckoutAsync
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(providerTransactionId, provider.ToLower());
            if (payment == null) return false;
            if (payment.Status == PaymentStatus.Success) return true; // idempotent — safe for webhook retries

            var order = await _orderRepo.GetOrderWithItemsAsync(payment.OrderId)
                        ?? throw new Exception("Order not found");

            // ─────────────────────────────────────────────────────────────────
            // BUG 3 FIX: Original code set payment.Status and order.Status
            // inside ExecuteInTransactionAsync but never called SaveChangesAsync
            // afterwards. The DB was never actually updated, so stock stayed
            // the same and the order stayed "pending" after every payment.
            // ─────────────────────────────────────────────────────────────────
            await _paymentRepo.ExecuteInTransactionAsync(async () =>
            {
                var mapper = StockService.MapperConfig.GetMapper();
                var itemDtos = mapper.Map<IEnumerable<OrderItemDTO>>(order.OrderItems);

                // Validate stock and reduce quantities in Products table
                await _stockService.ValidateAndReduceAsync(itemDtos);

                payment.Status = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;

                // BUG 3 FIX: SaveChangesAsync was missing — without this line
                // none of the status changes above were ever persisted to the DB.
                await _paymentRepo.SaveChangesAsync();
            });

            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(string provider, string transactionId)
        {
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(transactionId, provider.ToLower())
                          ?? throw new Exception("Payment not found");

            if (payment.Status == PaymentStatus.Success) return true; // idempotent

            if (provider.ToLower() == "stripe")
            {
                throw new Exception("Stripe payments are confirmed via webhook. Please check order status instead.");
            }

            var strategy = _strategyFactory.GetStrategy(provider);

            var executed = await strategy.ExecutePaymentAsync(transactionId);
            if (!executed)
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepo.SaveChangesAsync();
                return false;
            }

            var order = await _orderRepo.GetOrderWithItemsAsync(payment.OrderId)
                        ?? throw new Exception("Order not found");

            try
            {
                await _paymentRepo.ExecuteInTransactionAsync(async () =>
                {
                    var mapper = StockService.MapperConfig.GetMapper();
                    var itemDtos = mapper.Map<IEnumerable<OrderItemDTO>>(order.OrderItems);
                    await _stockService.ValidateAndReduceAsync(itemDtos);

                    payment.Status = PaymentStatus.Success;
                    order.Status = OrderStatus.Paid;

                    // SaveChangesAsync included here too for consistency
                    await _paymentRepo.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepo.SaveChangesAsync();
                throw new Exception("Payment captured but order could not be completed. Refund required.", ex);
            }

            return true;
        }
    }
}