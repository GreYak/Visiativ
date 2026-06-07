using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace BasketService.Infrastructure
{
    public class BasketItemRepository : IBasketItemRepository
    {
        private readonly string _connectionString;

        public BasketItemRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["basketdb"].ConnectionString;
        }

        public IEnumerable<BasketItem> Get()
        {
            var items = new List<BasketItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = "SELECT ProductId, Name, Price, Quantity FROM BasketItems";

                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new BasketItem(
                            productId: reader.GetGuid(0),
                            name:      reader.GetString(1),
                            price:     reader.GetDecimal(2),
                            quantity:  reader.GetInt32(3)
                        ));
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Insère l'item s'il n'existe pas encore, ou met à jour sa quantité et son prix.
        /// La quantité passée est la quantité finale souhaitée — le calcul d'accumulation
        /// est de la responsabilité du domaine (<see cref="Domain.AddItemToBasket"/>).
        /// </summary>
        public void EnsureBasketItem(BasketItem item)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"
                    MERGE BasketItems AS target
                    USING (SELECT @ProductId AS ProductId) AS source
                        ON target.ProductId = source.ProductId
                    WHEN MATCHED THEN
                        UPDATE SET Quantity = @Quantity, Price = @Price
                    WHEN NOT MATCHED THEN
                        INSERT (ProductId, Name, Price, Quantity)
                        VALUES (@ProductId, @Name, @Price, @Quantity);";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", item.ProductId);
                    command.Parameters.AddWithValue("@Name",      item.Name);
                    command.Parameters.AddWithValue("@Price",     item.Price);
                    command.Parameters.AddWithValue("@Quantity",  item.Quantity);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Clear()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("DELETE FROM BasketItems", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
