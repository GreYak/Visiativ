using BasketService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Http;

namespace BasketService.Controllers
{
    [RoutePrefix("api/basket")]
    public class BasketController : ApiController
    {
        private static List<BasketItem> _basket = new List<BasketItem>();

        // GET api/basket
        [HttpGet]
        [Route("")]
        public IEnumerable<BasketItem> Get()
        {
            return _basket;
        }

        // POST api/basket/add
        [HttpPost]
        [Route("add")]
        public IHttpActionResult Add(BasketItem item)
        {
            if (item == null)
                return BadRequest("Item invalide");

            _basket.Add(item);
            return Ok();
        }

        // POST api/basket/clear
        [HttpPost]
        [Route("clear")]
        public IHttpActionResult Clear()
        {
            _basket.Clear();
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
