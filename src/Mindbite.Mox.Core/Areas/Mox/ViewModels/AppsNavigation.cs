using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.ViewModels
{
    public class AppsNavigationViewModel
    {
        public Configuration.Config Config { get; set; }
        public Configuration.Apps.App App { get; set; }
        public ISet<string> Roles { get; set; }

        public AppsNavigationViewModel(ViewComponent viewComponent, Configuration.Config config, ISet<string> roles)
        {
            var area = viewComponent.RouteData.Values["Area"]?.ToString();
            var controller = viewComponent.RouteData.Values["Controller"]?.ToString();

            this.Config = config;
            this.App = this.Config.Apps.FirstOrDefault(x => x.Areas.Contains(area));
            this.Roles = roles;
        }
    }
}
