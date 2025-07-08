namespace AldiApi
{
    public class Product
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string UrlSlugText { get; set; } = string.Empty;
        public string QuantityUnit { get; set; } = string.Empty;
        public string SellingSize { get; set; } = string.Empty;
        public int PriceAmountRelevant { get; set; }
        public string PriceAmountRelevantDisplay { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
