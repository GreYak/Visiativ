using BasketService.Domain;
using BasketService.Infrastructure;
using BasketService.Models;
using System;
using System.Configuration;
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

        public BasketController()
        {
            var repository = new BasketItemRepository();
            _getBasket       = new GetBasket(repository);
            _addItemToBasket = new AddItemToBasket(repository);
            _deleteBasket    = new DeleteBasket(repository);
        }

        // GET api/basket
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Get()
        {
            var items = await _getBasket.HandleAsync();
            return Ok(items);
        }

        // POST api/basket/add
        [HttpPost]
        [Route("add")]
        public async Task<IHttpActionResult> Add(BasketItem item)
        {
            if (item == null)
                return BadRequest("Item invalide");

            await _addItemToBasket.HandleAsync(item);
            return Ok();
        }

        // POST api/basket/clear
        [HttpPost]
        [Route("clear")]
        public async Task<IHttpActionResult> Clear()
        {
            await _deleteBasket.HandleAsync();
            return Ok();
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
