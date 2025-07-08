using AldiScraper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text.Json;

namespace AldiScraper
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string[] categoryKeys = new[]
            {
            "940000000", //meat and seafood
            "950000000", //fruit and veg,
            "930000000", //deli
            "960000000", //dairy and eggs
            "970000000", //pantry,
            "920000000", //bakery,
            "980000000", //freezer
            "1000000000", //drinks
            "1040000000", //health and beauty
            "1050000000", //household
            "1588161408332087", //snacks and confectionery
            };

            await SyncAllProductsAsync(categoryKeys, args[0]);
        }

        public static async Task SyncAllProductsAsync(string[] categoryKeys, string connectionString)
        {
            Console.WriteLine("Gathering ALDI products...");
            const int pageSize = 30;
            var allProducts = new List<Product>();
            using var http = new HttpClient();

            foreach (var category in categoryKeys)
            {
                int offset = 0;
                while (true)
                {
                    var url =
                        $"https://api.aldi.com.au/v3/product-search" +
                        $"?currency=AUD&serviceType=walk-in" +
                        $"&categoryKey={category}" +
                        $"&limit={pageSize}&offset={offset}" +
                        $"&getNotForSaleProducts=1" +
                        $"&sort=name_asc&testVariant=A&servicePoint=G700";

                    var resp = await http.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        string errorContent = await resp.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error fetching data for category {category} at offset {offset}:");
                        Console.WriteLine($"Status: {resp.StatusCode} ({(int)resp.StatusCode})");
                        Console.WriteLine($"Response: {errorContent}");
                        break;
                    }

                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());

                    var items = doc.RootElement
                                   .GetProperty("data")
                                   .Deserialize<List<Product>>();

                    if (items == null || items.Count == 0)
                        break;

                    allProducts.AddRange(items);

                    // done with page
                    if (items.Count < pageSize)
                        break;

                    offset += pageSize;
                    await Task.Delay(150); // delay so Aldi doesn't rate limit us lol
                }
            }

            // find dupes, this occurs cos some skus exist in different categories
            var duplicateSkus = allProducts
                .GroupBy(p => p.Sku)
                .Where(g => g.Count() > 1)
                .Select(g => new { Sku = g.Key, Count = g.Count(), Products = g.ToList() });

            // nuke the dupes
            allProducts = allProducts
                .GroupBy(p => p.Sku)
                .Select(g => g.First())
                .ToList();

            //db stuff below
            await using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();

            const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS ProductTable (
            Sku                     TEXT PRIMARY KEY,
            Name                    TEXT,
            BrandName               TEXT,
            UrlSlugText             TEXT,
            QuantityUnit            TEXT,
            SellingSize             TEXT,
            PriceAmountRelevant     INTEGER,    
            PriceAmountRelevantDisplay TEXT,
            ImageUrl                TEXT,
            UpdatedTime             TEXT     
        );
        """;

            // create the table
            await new SqliteCommand(CreateTableSql, conn, tx).ExecuteNonQueryAsync();

            // clear previous snapshot
            await new SqliteCommand("DELETE FROM ProductTable;", conn, tx).ExecuteNonQueryAsync();

            // insert sql
            const string InsertSql = """
        INSERT INTO ProductTable
        (Sku, Name, BrandName, UrlSlugText, QuantityUnit, SellingSize,
         PriceAmountRelevant, PriceAmountRelevantDisplay, ImageUrl, UpdatedTime)
        VALUES (@Sku,@Name,@BrandName,@UrlSlugText,@QuantityUnit,@SellingSize,
                @PriceAmountRelevant,@PriceAmountRelevantDisplay,@ImageUrl,@UpdatedTime);
        """;
            await using var cmd = new SqliteCommand(InsertSql, conn, tx);

            // add parameters
            cmd.Parameters.AddRange(new[]
            {
            new SqliteParameter("@Sku", SqliteType.Text),
            new SqliteParameter("@Name", SqliteType.Text),
            new SqliteParameter("@BrandName", SqliteType.Text),
            new SqliteParameter("@UrlSlugText", SqliteType.Text),
            new SqliteParameter("@QuantityUnit", SqliteType.Text),
            new SqliteParameter("@SellingSize", SqliteType.Text),
            new SqliteParameter("@PriceAmountRelevant", SqliteType.Integer),
            new SqliteParameter("@PriceAmountRelevantDisplay", SqliteType.Text),
            new SqliteParameter("@ImageUrl", SqliteType.Text),
            new SqliteParameter("@UpdatedTime", SqliteType.Text)
            });

            foreach (var p in allProducts)
            {
                cmd.Parameters["@Sku"].Value = p.Sku;
                cmd.Parameters["@Name"].Value = p.Name;
                cmd.Parameters["@BrandName"].Value = p.BrandName == null ? "" : p.BrandName;
                cmd.Parameters["@UrlSlugText"].Value = p.UrlSlugText == null ? "" : p.UrlSlugText;
                cmd.Parameters["@QuantityUnit"].Value = p.QuantityUnit == null ? "" : p.QuantityUnit;
                cmd.Parameters["@SellingSize"].Value = p.SellingSize == null ? "" : p.SellingSize;
                cmd.Parameters["@PriceAmountRelevant"].Value = p.Price.AmountRelevant;
                cmd.Parameters["@PriceAmountRelevantDisplay"].Value = p.Price.AmountRelevantDisplay == null ? "" : p.Price.AmountRelevantDisplay;
                cmd.Parameters["@ImageUrl"].Value = p.Assets?.FirstOrDefault()?.Url ?? "";
                cmd.Parameters["@UpdatedTime"].Value = DateTime.UtcNow.ToString("O");

                CheckParameters(cmd, p.Sku);

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting product {p.Sku}: {ex.Message}");
                    continue;
                }

            }

            await tx.CommitAsync();
            Console.WriteLine($"Total products found: {allProducts.Count}");
        }
        static void CheckParameters(SqliteCommand cmd, string sku)
        {
            foreach (SqliteParameter p in cmd.Parameters)
            {
                if (p.Value is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"SKU {sku} ➜ Parameter {p.ParameterName} is NULL");
                    Console.ResetColor();
                }
            }
        }

    }
}
