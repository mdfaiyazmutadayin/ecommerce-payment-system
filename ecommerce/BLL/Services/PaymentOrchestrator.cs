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

            // Store the payment record
            _paymentRepo.Create(new Payment
            {
                OrderId = orderId,
                Provider = provider.ToLower(),
                TransactionId = transactionId,
                RawResponse = rawResponse,
                Status = PaymentStatus.Pending
            });
            await _paymentRepo.SaveChangesAsync();

            return (transactionId, provider, rawResponse);
        }

        // NEW: Get current payment status from our DB (or optionally from provider)
        public async Task<string> GetPaymentStatusAsync(string provider, string transactionId)
        {
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(transactionId, provider.ToLower());
            if (payment == null) return "not_found";
            return payment.Status.ToString().ToLower(); // pending, success, failed
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
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(providerTransactionId, provider.ToLower());
            if (payment == null) return false;
            if (payment.Status == PaymentStatus.Success) return true; // idempotent

            var order = await _orderRepo.GetOrderWithItemsAsync(payment.OrderId)
                        ?? throw new Exception("Order not found");

            await _paymentRepo.ExecuteInTransactionAsync(async () =>
            {
                var mapper = StockService.MapperConfig.GetMapper();
                var itemDtos = mapper.Map<IEnumerable<OrderItemDTO>>(order.OrderItems);
                await _stockService.ValidateAndReduceAsync(itemDtos);

                payment.Status = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;
            });

            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(string provider, string transactionId)
        {
            var payment = await _paymentRepo.GetByTransactionAndProviderAsync(transactionId, provider.ToLower())
                          ?? throw new Exception("Payment not found");

            if (payment.Status == PaymentStatus.Success) return true; // idempotent

            // For Stripe, we should NOT call Execute/Query because the webhook already updates status.
            // Instead, we can just return the current status (or throw an error).
            if (provider.ToLower() == "stripe")
            {
                // Since Stripe uses webhooks, this endpoint is not meant for manual confirmation.
                // We'll just return the current status from DB (or throw a clear error).
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

            var verified = await strategy.QueryPaymentAsync(transactionId);
            if (!verified)
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepo.SaveChangesAsync();
                return false;
            }

            var order = await _orderRepo.GetOrderWithItemsAsync(payment.OrderId)
                        ?? throw new Exception("Order not found");

            await _paymentRepo.ExecuteInTransactionAsync(async () =>
            {
                var mapper = StockService.MapperConfig.GetMapper();
                var itemDtos = mapper.Map<IEnumerable<OrderItemDTO>>(order.OrderItems);
                await _stockService.ValidateAndReduceAsync(itemDtos);

                payment.Status = PaymentStatus.Success;
                order.Status = OrderStatus.Paid;
            });

            return true;
        }
    }
}