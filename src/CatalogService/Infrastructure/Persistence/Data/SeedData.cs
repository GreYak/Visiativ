using CatalogService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Data;

public static class SeedData
{
    public static async Task InitializeAsync(CatalogDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var rng = new Random();
        int Stock() => rng.Next(0, 16); // [0, 15]

        context.Products.AddRange(
            Product.Create("Laptop Pro 15",      "Ordinateur portable haute performance — Intel Core i9, 32 Go RAM, SSD 1 To", 1299.99m, Stock()),
            Product.Create("Souris sans fil",    "Souris ergonomique 2.4 GHz — autonomie 18 mois",                              29.99m, Stock()),
            Product.Create("Clavier mécanique",  "Switch Cherry MX Blue, rétroéclairage RGB, disposition AZERTY",               89.99m, Stock()),
            Product.Create("Moniteur 27 pouces", "Dalle IPS QHD 2560×1440, 165 Hz, temps de réponse 1 ms",                    399.99m, Stock()),
            Product.Create("Casque audio USB",   "Son surround 7.1 virtuel, micro antibruit, compatible PC et Mac",             79.99m, Stock()),
            Product.Create("Webcam Full HD",     "1080p 30 fps, autofocus, correction lumière automatique",                    59.99m, Stock())
        );

        await context.SaveChangesAsync();
    }
}
