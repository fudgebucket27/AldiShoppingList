using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AldiScraper
{
    public class PriceInfo
    {
        [JsonPropertyName("amountRelevant")]
        public int AmountRelevant { get; set; }

        [JsonPropertyName("amountRelevantDisplay")]
        public string AmountRelevantDisplay { get; set; }
    }
}
