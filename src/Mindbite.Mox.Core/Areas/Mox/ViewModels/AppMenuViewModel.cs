using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.ViewModels
{
    public class AppMenuViewModel
    {
        public Configuration.Apps.App App { get; set; }
        public bool AppFound { get; set; }
    }
}
