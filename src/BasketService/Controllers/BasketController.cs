using BasketService.Domain;
using BasketService.Models;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace BasketService.Controllers
{
    [RoutePrefix("api/basket")]
    public class BasketController : ApiController
    {
        private readonly GetBasket _getBasket;
        private readonly AddItemToBasket _addItemToBasket;
        private readonly DeleteBasket _deleteBasket;

        public BasketController(GetBasket getBasket, AddItemToBasket addItemToBasket, DeleteBasket deleteBasket)
        {
            _getBasket       = getBasket;
            _addItemToBasket = addItemToBasket;
            _deleteBasket    = deleteBasket;
        }

        // GET api/basket
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Get()
        {
            var items = await _getBasket.HandleAsync();
            return Ok(items);
        }

        // POST api/basket/add  (body JSON : { productId, quantity, limitMax? })
        [HttpPost]
        [Route("add")]
        public async Task<IHttpActionResult> Add(AddItemRequest request)
        {
            if (request == null)
                return BadRequest("Requête invalide.");

            if (request.LimitMax.HasValue && request.LimitMax.Value <= 0)
                return BadRequest("Le paramètre limitMax doit être strictement positif.");

            try
            {
                await _addItemToBasket.HandleAsync(request.ToBasketItem(), request.LimitMax);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/basket
        [HttpDelete]
        [Route("")]
        public async Task<IHttpActionResult> Clear()
        {
            await _deleteBasket.HandleAsync();
            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET api/basket/alive — sonde de liveness pour Aspire
        [HttpGet]
        [Route("alive")]
        public IHttpActionResult IsAlive() => Ok(true);
    }
}
