using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Product : Base
    {
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty; // unique
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;

        public string Status { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
