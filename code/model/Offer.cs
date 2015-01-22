using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectFood.Models
{
    public class Offer
    {
        public int ID { get; set; }
        public ICollection<Item> GenericItems { get; set; }
        public string Heading { get; set; }
        public string Store_ID { get; set; }
        public string Store { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:ddd d-M}", ApplyFormatInEditMode = true)]
        public DateTime Begin { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:ddd d- M}", ApplyFormatInEditMode = true)]
        public DateTime End { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public string Image_URL { get; set; }

        public Offer()
        {
            GenericItems = new List<Item>();
        }
    }


}