using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;
using System.Net;

namespace Visiativ.ApiService.Endpoints;

public static class BasketEndpoints
{
    public static void MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/basket").WithTags("Basket");

        // GET /basket
        group.MapGet("/", async (IBasketClient basket, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await basket.GetBasketAsync(ct));
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }
        })
        .WithName("BFF_GetBasket")
        .WithSummary("Retourne le contenu du panier.")
        .Produces<IEnumerable<BasketItem>>()
        .Produces(StatusCodes.Status503ServiceUnavailable);

        // DELETE /basket
        group.MapDelete("/", async (IBasketClient basket, CancellationToken ct) =>
        {
            try
            {
                await basket.ClearBasketAsync(ct);
                return Results.NoContent();
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }
        })
        .WithName("BFF_ClearBasket")
        .WithSummary("Vide le panier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        // POST /basket/items
        group.MapPost("/items", async (
            AddItemRequest req,
            ICatalogClient catalog,
            IBasketClient basket,
            CancellationToken ct) =>
        {
            // 1. Récupération du produit — peut échouer si CatalogService est indisponible
            ProductResponse? product;
            try
            {
                product = await catalog.GetProductByIdAsync(req.ProductId, ct);
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }

            // 2. Validations métier côté catalogue
            if (product is null)
                return Results.BadRequest($"Le produit '{req.ProductId}' est introuvable.");

            if (product.Stock < req.Quantity)
                return Results.BadRequest(
                    $"Stock insuffisant. Disponible : {product.Stock}, demandé : {req.Quantity}.");

            // 3. Ajout au panier — limitMax = stock catalogue pour éviter de dépasser le stock
            //    disponible après accumulation des quantités déjà en panier.
            var item = new BasketItem(product.Id, product.Name, product.Price, req.Quantity);
            try
            {
                await basket.AddItemAsync(item, limitMax: product.Stock, ct);
            }
            catch (RemoteConflictException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (RemoteValidationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }

            return Results.Ok();
        })
        .WithName("BFF_AddItemToBasket")
        .WithSummary("Ajoute un produit au panier après vérification du stock.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status503ServiceUnavailable);
    }
}
