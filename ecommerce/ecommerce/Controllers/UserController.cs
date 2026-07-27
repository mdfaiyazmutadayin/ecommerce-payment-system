using BLL;
using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace ecommerce.Controllers
{
    [RoutePrefix("api/User")]
    public class UserController : ApiController
    {
        [HttpPost]
        [Route("register")]
        public async Task<HttpResponseMessage> Register(RegisterUserDto dto)
        {
            try
            {
                var user = await ServiceFactory.UserData().RegisterAsync(dto);
                return Request.CreateResponse(HttpStatusCode.OK, user);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("login")]
        public async Task<HttpResponseMessage> Login(LoginDto dto)
        {
            try
            {
                var user = await ServiceFactory.UserData().LoginAsync(dto);
                return Request.CreateResponse(HttpStatusCode.OK, user);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ex.Message);
            }
        }
    }
}
