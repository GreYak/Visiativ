namespace CatalogService.Domain
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int Stock { get; private set; }

        /// <summary>Constructeur sans paramètre requis par EF Core pour la matérialisation.</summary>
        private Product() { }

        public static Product Create(string name, string description, decimal price, int stock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (price < 0) throw new ArgumentException("Price cannot be negative.", nameof(price));
            if (stock < 0) throw new ArgumentException("Stock cannot be negative.", nameof(stock));

            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Price = price,
                Stock = stock
            };
        }
    }
}
