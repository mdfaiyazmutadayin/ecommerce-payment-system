using AutoMapper;
using BLL.DTOs;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services
{
    
    public class StockService // Ensure IStockService matches this if you have it

    {
        public static class MapperConfig
        {
            public static Mapper GetMapper()
            {
                var config = new MapperConfiguration(cfg =>
                {
                    // E-commerce Mappings
                    cfg.CreateMap<Order, CreateOrderDto>().ReverseMap();
                    cfg.CreateMap<OrderItem, OrderItemDTO>().ReverseMap();
                    cfg.CreateMap<Product, ProductDTO>().ForMember(dto => dto.ProductStatus, opt => opt.MapFrom(p => p.Status)).ReverseMap()
                .ForMember(p => p.Status, opt => opt.MapFrom(dto => dto.ProductStatus));

                });

                return new Mapper(config);
            }
        }
        // 1. Inject the repository instead of the DbContext!
        private readonly IProductRepo _productRepo;

        public StockService(IProductRepo productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task ValidateAndReduceAsync(IEnumerable<OrderItemDTO> itemDtos)
        {
            var mapper = MapperConfig.GetMapper();
            var items = mapper.Map<IEnumerable<OrderItem>>(itemDtos);

            foreach (var item in items)
            {
                // 2. Use the repository to get the product, NO Entity Framework required here!
                var product = _productRepo.Get(item.ProductId);

                if (product == null)
                    throw new Exception($"Product {item.ProductId} not found");

                if (product.Stock < item.Quantity)
                    throw new Exception($"Insufficient stock for product {product.Id}");

                // 3. Call the method we added to the repository earlier
                await _productRepo.ReduceStockAsync(item.ProductId, item.Quantity);
            }
        }
    }
}