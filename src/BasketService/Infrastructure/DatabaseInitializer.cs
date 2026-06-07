using System.Configuration;
using System.Data.SqlClient;

namespace BasketService.Infrastructure
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["basketdb"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var sql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = 'BasketItems'
                    )
                    BEGIN
                        CREATE TABLE BasketItems (
                            ProductId   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                            Name        NVARCHAR(200)    NOT NULL,
                            Price       DECIMAL(18,2)    NOT NULL,
                            Quantity    INT              NOT NULL
                        )
                    END";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
