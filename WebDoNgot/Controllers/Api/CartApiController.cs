using Microsoft.AspNetCore.Mvc;
using WebDoNgot.Extensions;
using WebDoNgot.Models;

namespace WebDoNgot.Controllers.Api
{
    [Route("api/cart")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        [HttpGet("count")]
        public IActionResult Count()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return Ok(new { count = cart.GetTotalQuantity() });
        }
    }
}