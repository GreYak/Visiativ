using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Endpoints;

public static class BasketEndpoints
{
    public static void MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/basket").WithTags("Basket");

        // GET /basket
        group.MapGet("/", async (IBasketClient basket, CancellationToken ct) =>
            Results.Ok(await basket.GetBasketAsync(ct)))
            .WithName("BFF_GetBasket")
            .WithSummary("Retourne le contenu du panier.")
            .Produces<IEnumerable<BasketItem>>();

        // DELETE /basket
        group.MapDelete("/", async (IBasketClient basket, CancellationToken ct) =>
        {
            await basket.ClearBasketAsync(ct);
            return Results.NoContent();
        })
        .WithName("BFF_ClearBasket")
        .WithSummary("Vide le panier.")
        .Produces(StatusCodes.Status204NoContent);

        // POST /basket/items
        group.MapPost("/items", async (
            AddItemRequest req,
            ICatalogClient catalog,
            IBasketClient basket,
            CancellationToken ct) =>
        {
            // Vérification existence + stock dans le catalogue
            var product = await catalog.GetProductByIdAsync(req.ProductId, ct);

            if (product is null)
                return Results.BadRequest($"Le produit '{req.ProductId}' est introuvable.");

            if (product.Stock < req.Quantity)
                return Results.BadRequest(
                    $"Stock insuffisant. Disponible : {product.Stock}, demandé : {req.Quantity}.");

            // Ajout au panier avec les données du produit
            var item = new BasketItem(product.Id, product.Name, product.Price, req.Quantity);
            await basket.AddItemAsync(item, ct);

            return Results.Ok();
        })
        .WithName("BFF_AddItemToBasket")
        .WithSummary("Ajoute un produit au panier après vérification du stock.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
