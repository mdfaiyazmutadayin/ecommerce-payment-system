using BLL.DTOs;
using DAL.Interfaces;
using DAL.Models;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class CategoryService
    {
        private readonly ICategoryRepo _categoryRepo;
        private const string CacheKey = "CategoryTree:Flat";
        private static readonly System.TimeSpan CacheDuration = System.TimeSpan.FromMinutes(30);

        public CategoryService(ICategoryRepo categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private List<Category> GetCachedFlatCategories()
        {
            var db = RedisCacheProvider.Db;
            var cached = db.StringGet(CacheKey);

            if (cached.HasValue)
                return JsonConvert.DeserializeObject<List<Category>>(cached, _jsonSettings);

            var fresh = _categoryRepo.GetAllFlat();
            db.StringSet(CacheKey, JsonConvert.SerializeObject(fresh, _jsonSettings), CacheDuration);
            return fresh;
        }

        public void InvalidateCache() => RedisCacheProvider.Db.KeyDelete(CacheKey);

        public List<int> GetDescendantCategoryIds(int rootCategoryId)
        {
            var allCategories = GetCachedFlatCategories();
            var childrenLookup = allCategories
                .Where(c => c.ParentCategoryId.HasValue)
                .ToLookup(c => c.ParentCategoryId.Value);

            var result = new List<int>();
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(rootCategoryId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                if (!visited.Add(currentId)) continue;

                result.Add(currentId);

                foreach (var child in childrenLookup[currentId])
                    stack.Push(child.Id);
            }

            return result;
        }

        public List<CategoryTreeDto> BuildFullTree()
        {
            var allCategories = GetCachedFlatCategories();
            var childrenLookup = allCategories
                .Where(c => c.ParentCategoryId.HasValue)
                .ToLookup(c => c.ParentCategoryId.Value);

            CategoryTreeDto BuildNode(Category c) => new CategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                Children = childrenLookup[c.Id].Select(BuildNode).ToList()
            };

            return allCategories
                .Where(c => !c.ParentCategoryId.HasValue)
                .Select(BuildNode)
                .ToList();
        }

        public List<ProductDTO> GetRelatedProducts(int categoryId)
        {
            var categoryIds = GetDescendantCategoryIds(categoryId);
            var products = _categoryRepo.GetProductsByCategoryIds(categoryIds);

            var mapper = StockService.MapperConfig.GetMapper();
            return products.Select(p => mapper.Map<ProductDTO>(p)).ToList();
        }

        public bool CreateCategory(CreateCategoryDto dto)
        {
            var result = _categoryRepo.Create(new Category
            {
                Name = dto.Name,
                ParentCategoryId = dto.ParentCategoryId
            });

            if (result) InvalidateCache();
            return result;
        }
    }
}