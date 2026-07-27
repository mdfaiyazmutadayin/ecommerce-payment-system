using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CreateCategoryDto
    {
        public string Name { get; set; }
        public int? ParentCategoryId { get; set; }
    }
}
