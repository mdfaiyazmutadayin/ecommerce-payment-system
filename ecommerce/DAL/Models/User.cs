using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class User : Base
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // unique
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
