using AutoMapper;
using BLL.DTOs;
using DAL.Enums;
using DAL.Models;
using DAL.Interfaces; // Use interfaces for DI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class OrderService // Ensure you implement IOrderService if you have one
    {
        // FIXED: Added variable names (_orderRepo, _productRepo) and used Interfaces
        private readonly IOrderRepo _orderRepo;
        private readonly IProductRepo _productRepo;
        private readonly IMapper _mapper;

        public OrderService(
            IOrderRepo orderRepo,
            IProductRepo productRepo,
            IMapper mapper)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _mapper = mapper;
        }

        public async Task<int> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();

            // FIXED: Call the instance variable _productRepo
            var products = await _productRepo.GetActiveProductsDictionaryAsync(productIds);

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending
            };

            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                    throw new Exception($"Product {itemDto.ProductId} not found or inactive");

                var orderItem = _mapper.Map<OrderItem>(itemDto);

                orderItem.Price = product.Price;
                orderItem.Subtotal = product.Price * itemDto.Quantity;

                // FIXED: The property on the Order model is 'OrderItems', not 'Items'
                order.OrderItems.Add(orderItem);
                total += orderItem.Subtotal;
            }

            order.TotalAmount = total;

            // FIXED: Use instance variable _orderRepo. In EF6, Add is synchronous.
            _orderRepo.Add(order);
            await _orderRepo.SaveChangesAsync();

            return order.Id;
        }
    }
}