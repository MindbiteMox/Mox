using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SelectMenuAttribute : Attribute
    {
        public string MenuId { get; set; }

        public SelectMenuAttribute(string menuId)
        {
            this.MenuId = menuId;
        }
    }
}
