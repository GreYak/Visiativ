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

                // 1. Crée la table si elle n'existe pas encore (schema minimal : ProductId + Quantity)
                var createSql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = 'BasketItems'
                    )
                    BEGIN
                        CREATE TABLE BasketItems (
                            ProductId   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                            Quantity    INT              NOT NULL
                        )
                    END";

                using (var cmd = new SqlCommand(createSql, connection))
                    cmd.ExecuteNonQuery();

                // 2. Migration : supprime les colonnes Name et Price si elles existent encore
                //    (tables créées avec l'ancien schéma)
                var dropNameSql = @"
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Name'
                    )
                    BEGIN
                        ALTER TABLE BasketItems DROP COLUMN Name;
                    END";

                var dropPriceSql = @"
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Price'
                    )
                    BEGIN
                        ALTER TABLE BasketItems DROP COLUMN Price;
                    END";

                using (var cmd = new SqlCommand(dropNameSql, connection))
                    cmd.ExecuteNonQuery();

                using (var cmd = new SqlCommand(dropPriceSql, connection))
                    cmd.ExecuteNonQuery();
            }
        }
    }
}
