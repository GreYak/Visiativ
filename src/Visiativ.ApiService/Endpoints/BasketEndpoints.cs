using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Clients;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Endpoints;

public static class BasketEndpoints
{
    public static void MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/basket").WithTags("Basket");

        // GET /basket
        group.MapGet("/", async (IBasketClient basket, ICatalogClient catalog, CancellationToken ct) =>
        {
            List<BasketItemDto> dtos;
            bool isPartial;
            try
            {
                (dtos, isPartial) = await FetchAndJoinAsync(basket, catalog, ct);
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }

            return isPartial
                ? Results.Json(dtos, statusCode: StatusCodes.Status207MultiStatus)
                : Results.Ok(dtos);
        })
        .WithName("BFF_GetBasket")
        .WithSummary("Retourne le contenu du panier, enrichi des informations catalogue.")
        .Produces<IEnumerable<BasketItemDto>>()
        .Produces(StatusCodes.Status207MultiStatus)
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
            AddItemRequestDto req,
            ICatalogClient catalog,
            IBasketClient basket,
            CancellationToken ct) =>
        {
            // 1. Récupération du produit — peut échouer si CatalogService est indisponible
            ProductExt? product;
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
            var item = new BasketItemExt(product.Id, req.Quantity);
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

        // POST /basket/pay
        group.MapPost("/pay", async (IBasketClient basket, ICatalogClient catalog, CancellationToken ct) =>
        {
            // 1. Récupération et consolidation panier + catalogue
            List<BasketItemDto> items;
            bool isPartial;
            try
            {
                (items, isPartial) = await FetchAndJoinAsync(basket, catalog, ct);
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }

            // 2. Tous les produits du panier doivent exister dans le catalogue
            if (isPartial)
                return Results.BadRequest(
                    "Un ou plusieurs articles du panier sont introuvables dans le catalogue.");

            // 3. Vérification des stocks — chaque quantité doit rester dans le stock disponible
            var horsStock = items.FirstOrDefault(i => i.Quantity > i.Stock);
            if (horsStock is not null)
                return Results.BadRequest(
                    $"Stock insuffisant pour '{horsStock.Name}' : demandé {horsStock.Quantity}, disponible {horsStock.Stock}.");

            // 4. Calcul du montant total
            var total = items.Sum(i => i.Quantity * i.Price);

            // 5. Vider le panier — le paiement est considéré comme effectué
            try
            {
                await basket.ClearBasketAsync(ct);
            }
            catch (ServiceUnavailableException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: $"Le service '{ex.ServiceName}' est temporairement indisponible.");
            }

            return Results.Ok(new PaymentDto(total));
        })
        .WithName("BFF_PayBasket")
        .WithSummary("Valide le panier, calcule le total et vide le panier.")
        .Produces<PaymentDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Récupère les articles du panier et les produits du catalogue, puis les consolide en <see cref="BasketItemDto"/>.
    /// Les articles dont le <c>ProductId</c> est absent du catalogue sont ignorés (<paramref name="isPartial"/> = <c>true</c>).
    /// Peut lever <see cref="ServiceUnavailableException"/> si l'un des services est indisponible.
    /// </summary>
    private static async Task<(List<BasketItemDto> Items, bool IsPartial)> FetchAndJoinAsync(
        IBasketClient basket, ICatalogClient catalog, CancellationToken ct)
    {
        var entries  = await basket.GetBasketAsync(ct);
        var products = await catalog.GetAllProductsAsync(ct);

        var productMap = products.ToDictionary(p => p.Id);
        var dtos       = new List<BasketItemDto>();
        var isPartial  = false;

        foreach (var item in entries)
        {
            if (productMap.TryGetValue(item.ProductId, out var product))
                dtos.Add(BasketItemDto.From(item, product));
            else
                isPartial = true;
        }

        return (dtos, isPartial);
    }
}
