﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AldiScraper
{
    public class ProductResponse
    {
        [JsonPropertyName("data")]
        public List<Product> Products { get; set; }
    }
}
