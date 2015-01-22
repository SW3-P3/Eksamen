using System;
using System.Collections.Generic;

namespace ProjectFood.Models
{
    public class Recipe
    {
        public string Titel { get; set; }
        public List<Item> Ingredients { get; set; }
        public int Minutes { get; set; }
        public string Instructions { get; set; }
    }
}