using BLL;
using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ecommerce.Controllers
{
    [RoutePrefix("api/Category")]
    public class CategoryController : ApiController
    {
        [HttpGet]
        [Route("tree")]
        public HttpResponseMessage GetTree()
        {
            var tree = ServiceFactory.CategoryData().BuildFullTree();
            return Request.CreateResponse(HttpStatusCode.OK, tree);
        }

        [HttpGet]
        [Route("{id}/related-products")]
        public HttpResponseMessage GetRelatedProducts(int id)
        {
            var products = ServiceFactory.CategoryData().GetRelatedProducts(id);
            return Request.CreateResponse(HttpStatusCode.OK, products);
        }

        [HttpPost]
        [Route("create")]
        public HttpResponseMessage Create(CreateCategoryDto dto)
        {
            if (dto == null) return Request.CreateResponse(HttpStatusCode.BadRequest, "Request body is required");
            var ok = ServiceFactory.CategoryData().CreateCategory(dto);
            return Request.CreateResponse(ok ? HttpStatusCode.OK : HttpStatusCode.BadRequest, ok);
        }
    }
}
