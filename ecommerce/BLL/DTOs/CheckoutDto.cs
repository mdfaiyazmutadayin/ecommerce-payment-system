using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CheckoutDto
    {
        public string Provider { get; set; } = string.Empty; // stripe | bkash

    }
}
