using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AldiScraper
{
    public class Product
    {
        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("brandName")]
        public string BrandName { get; set; }

        [JsonPropertyName("urlSlugText")]
        public string UrlSlugText { get; set; }

        [JsonPropertyName("quantityUnit")]
        public string QuantityUnit { get; set; }

        [JsonPropertyName("sellingSize")]
        public string SellingSize { get; set; }

        [JsonPropertyName("price")]
        public PriceInfo Price { get; set; }

        [JsonPropertyName("assets")]
        public List<Asset> Assets { get; set; }
    }
}
