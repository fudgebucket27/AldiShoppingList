using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.Json;

namespace AldiApi
{
    public class ProductService : IProductService
    {
        private readonly string _connectionString;

        public ProductService(IConfiguration configuration)
        {
         _connectionString = configuration.GetConnectionString("Aldi");
        }
        public async Task<string> GetProductsAsync(string name)
        {

            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            {
                var emptyList = Enumerable.Empty<Product>();
                return JsonSerializer.Serialize(emptyList);
            }

            const string sql = @"
            SELECT 
                Sku,
                Name,
                BrandName,
                UrlSlugText,
                QuantityUnit,
                SellingSize,
                PriceAmountRelevant,
                PriceAmountRelevantDisplay,
                ImageUrl,
                UpdatedTime
            FROM ProductTable
            WHERE Name LIKE @pattern;
        ";

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // Dapper maps each row to ProductDto for you
            var results = await conn.QueryAsync<Product>(
                sql,
                new { pattern = $"%{name}%" }
            );

            // one‐liner to JSON
            return JsonSerializer.Serialize(results);
        }
    }
}
