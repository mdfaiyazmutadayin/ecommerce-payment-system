using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using BLL;
using BLL.DTOs;

namespace ecommerce.Controllers
{
    [RoutePrefix("api/Order")]
    public class OrderController : ApiController
    {
        [HttpPost]
        [Route("create")]
        public async Task<HttpResponseMessage> Create(CreateOrderDto dto)
        {
            // TODO: replace hardcoded userId with the authenticated user's id once UserController/auth exists
            const int userId = 1;

            try
            {
                var orderId = await ServiceFactory.OrderData().CreateOrderAsync(userId, dto);
                return Request.CreateResponse(HttpStatusCode.OK, new { orderId });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}