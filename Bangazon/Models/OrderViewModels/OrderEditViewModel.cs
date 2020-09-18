using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bangazon.Models.OrderViewModels
{
    public class OrderEditViewModel
    {
        public Order Order { get; set; }
        public List<SelectListItem> PaymentTypes { get; set; } = new List<SelectListItem>();
    }
}
