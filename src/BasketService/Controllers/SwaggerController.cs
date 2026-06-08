using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace BasketService.Controllers
{
    public class SwaggerController : ApiController
    {
        private const string OpenApiSpec = @"{
  ""openapi"": ""3.0.1"",
  ""info"": { ""title"": ""BasketService"", ""version"": ""v1"" },
  ""paths"": {
    ""/api/basket"": {
      ""get"": {
        ""summary"": ""Retourne le contenu du panier"",
        ""responses"": {
          ""200"": { ""description"": ""Liste des items (vide si panier vide)"" },
          ""500"": { ""description"": ""Erreur technique"" }
        }
      },
      ""delete"": {
        ""summary"": ""Vide le panier"",
        ""responses"": {
          ""204"": { ""description"": ""Panier vidé"" },
          ""500"": { ""description"": ""Erreur technique"" }
        }
      }
    },
    ""/api/basket/add"": {
      ""post"": {
        ""summary"": ""Ajoute ou met à jour un item dans le panier"",
        ""requestBody"": {
          ""required"": true,
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""productId"": { ""type"": ""string"", ""format"": ""uuid"" },
                  ""quantity"":  { ""type"": ""integer"" },
                  ""limitMax"":  { ""type"": ""integer"", ""nullable"": true }
                },
                ""required"": [""productId"", ""quantity""]
              }
            }
          }
        },
        ""responses"": {
          ""200"": { ""description"": ""Ajout ou mise à jour réussi"" },
          ""400"": { ""description"": ""Requête invalide ou quantité ≤ 0"" },
          ""409"": { ""description"": ""Quantité accumulée dépasse limitMax"" },
          ""500"": { ""description"": ""Erreur technique"" }
        }
      }
    },
    ""/api/basket/alive"": {
      ""get"": {
        ""summary"": ""Sonde de liveness"",
        ""responses"": {
          ""200"": { ""description"": ""Service opérationnel"" }
        }
      }
    }
  }
}";

        // GET /swagger
        [HttpGet]
        [Route("swagger")]
        public HttpResponseMessage Index()
        {
            var html = @"<!DOCTYPE html>
<html>
<head>
  <title>BasketService — Swagger UI</title>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <link rel=""stylesheet"" href=""https://unpkg.com/swagger-ui-dist/swagger-ui.css""/>
</head>
<body>
  <div id=""swagger-ui""></div>
  <script src=""https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js""></script>
  <script>
    SwaggerUIBundle({
      spec: " + OpenApiSpec + @",
      dom_id: '#swagger-ui',
      presets: [SwaggerUIBundle.presets.apis, SwaggerUIBundle.SwaggerUIStandalonePreset],
      layout: 'BaseLayout'
    });
  </script>
</body>
</html>";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }
    }
}
