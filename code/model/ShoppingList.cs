
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace ProjectFood.Models{
    public class ShoppingList
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public ICollection<Item> Items { get; set;}
        public ShoppingList()
        {
            Items = new List<Item>();
        }
    }
}