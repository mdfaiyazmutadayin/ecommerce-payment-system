using BLL;
using BLL.DTOs;
using BLL.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ecommerce.Controllers
{
    [RoutePrefix("api/Product")]
    public class ProductController : ApiController
    {
        [HttpGet]
        [Route("all")]
        public HttpResponseMessage Get()
        {
            var data = ServiceFactory.ProductData().GetAll();
            return Request.CreateResponse(HttpStatusCode.OK, data);
        }

        [HttpGet]
        [Route("{id}")]
        public HttpResponseMessage Get(int id)
        {
            var data = ServiceFactory.ProductData().GetById(id);
            if (data == null) return Request.CreateResponse(HttpStatusCode.NotFound);
            return Request.CreateResponse(HttpStatusCode.OK, data);
        }

        [HttpPost]
        [Route("create")]
        public HttpResponseMessage Create(ProductDTO dto)
        {
            var ok = ServiceFactory.ProductData().Create(dto);
            return Request.CreateResponse(ok ? HttpStatusCode.OK : HttpStatusCode.BadRequest, ok);
        }

        [HttpPut]
        [Route("update")]
        public HttpResponseMessage Update(ProductDTO dto)
        {
            var ok = ServiceFactory.ProductData().Update(dto);
            return Request.CreateResponse(ok ? HttpStatusCode.OK : HttpStatusCode.BadRequest, ok);
        }

        [HttpDelete]
        [Route("{id}")]
        public HttpResponseMessage Delete(int id)
        {
            var ok = ServiceFactory.ProductData().Delete(id);
            return Request.CreateResponse(ok ? HttpStatusCode.OK : HttpStatusCode.NotFound, ok);
        }
    }
}