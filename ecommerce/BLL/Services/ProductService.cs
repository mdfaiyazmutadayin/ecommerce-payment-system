using BLL.DTOs;
using DAL.Interfaces;
using DAL.Models;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Services
{
    public class ProductService
    {
        private readonly IProductRepo _productRepo;

        public ProductService(IProductRepo productRepo)
        {
            _productRepo = productRepo;
        }

        public List<ProductDTO> GetAll()
        {
            var mapper = StockService.MapperConfig.GetMapper();
            return _productRepo.Get().Select(p => mapper.Map<ProductDTO>(p)).ToList();
        }

        public ProductDTO GetById(int id)
        {
            var product = _productRepo.Get(id);
            return product == null ? null : StockService.MapperConfig.GetMapper().Map<ProductDTO>(product);
        }

        public bool Create(ProductDTO dto)
        {
            var product = StockService.MapperConfig.GetMapper().Map<Product>(dto);
            return _productRepo.Create(product);
        }

        public bool Update(ProductDTO dto)
        {
            var product = StockService.MapperConfig.GetMapper().Map<Product>(dto);
            return _productRepo.Update(product);
        }

        public bool Delete(int id) => _productRepo.Delete(id);
    }
}