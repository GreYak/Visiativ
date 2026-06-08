using BasketService.Domain;
using BasketService.Models;
using System;
using System.Configuration;
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

        // POST api/basket/add  (body JSON : { productId, name, price, quantity, limitMax? })
        [HttpPost]
        [Route("add")]
        public async Task<IHttpActionResult> Add(AddItemRequest request)
        {
            if (request == null)
                return BadRequest("Requête invalide.");

            if (request.LimitMax.HasValue && request.LimitMax.Value < 0)
                return BadRequest("Le paramètre limitMax doit être positif.");

            try
            {
                await _addItemToBasket.HandleAsync(request.ToBasketItem(), request.LimitMax);
                return Ok();
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

        // GET api/basket/test — vérifie que la connection string est disponible
        // ⚠️ À supprimer avant de passer en production
        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["basketdb"]?.ConnectionString
                    ?? "Connection string 'basketdb' introuvable dans Web.config";
                return Ok(new { connectionString = cs });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
