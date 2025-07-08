using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AldiApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Search for products
        /// </summary>
        [HttpGet("search/{name}")]
        public async Task<ActionResult<List<string>>> GetProduct(string name)
        {
            var product = await _productService.GetProductsAsync(name);
            if (product == null)
            {
                return NotFound($"Could not find any products with that name...");
            }
            return Ok(product);
        }
    }
}
