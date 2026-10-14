using Jahhezni.Models;
using System.Collections.Generic;

namespace Jahhezni.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<User> Merchants { get; set; } // نستخدم موديل User لأن التجار مخزنين فيه
        public List<Product> Products { get; set; }
    }
}