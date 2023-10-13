using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Configuration.Apps
{
    public class AppPartial
    {
        public string Name { get; set; }
        public int Position { get; set; }
    }

    public class App
    {
        public string Name { get; set; }
        public string AppId { get; private set; }

        public AppMenus.AppMenuBuilder Menu { get; set; }
        public List<string> Areas { get; set; }

        public HashSet<string> Roles { get; set; }

        public AppPartial HeaderPartial { get; set; }

        public StaticIncludes.IncludeConfig StaticIncludes { get; private set; }
        public Func<Microsoft.AspNetCore.Mvc.ActionContext, AppMenus.AppMenuBuilder> ResolveActiveMenu { get; set; }

        public App(string name, string area = "", string appId = "")
        {
            this.Name = name;
            this.AppId = appId;
            this.Areas = new List<string>();
            this.Roles = new HashSet<string>();
            this.StaticIncludes = new StaticIncludes.IncludeConfig();

            this.Menu = new AppMenus.AppMenuBuilder();
            this.ResolveActiveMenu = _ => this.Menu;
        }

        public bool CanViewWithRoles(IEnumerable<string> roles)
        {
            if (roles == null)
                return true;
            return roles.Intersect(this.Roles).Count() == this.Roles.Count();
        }

        public IEnumerable<UI.Menu.MenuItem> GetCachedActiveMenu(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, IEnumerable<string> roles = null, bool tryMatchingActions = false)
        {
            var itemKey = $"MoxAppActiveMenu_{this.AppId}";

            if(actionContext.HttpContext.Items.TryGetValue(itemKey, out var item) && item is List<UI.Menu.MenuItem> menuItems)
            {
                return menuItems;
            }
            else
            {
                menuItems = ResolveActiveMenu(actionContext).Build(urlHelper, roles, tryMatchingActions).ToList();
                actionContext.HttpContext.Items[itemKey] = menuItems;
                return menuItems;
            }
        }
    }

    public class AppCollection : IEnumerable<App>
    {
        private List<App> Apps;

        public AppCollection()
        {
            this.Apps = new List<App>();
        }

        public IEnumerator<App> GetEnumerator()
        {
            return this.Apps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Apps.GetEnumerator();
        }

        public App Add(string name, string appId)
        {
            var app = new App(name, appId: appId);
            this.Apps.Add(app);
            return app;
        }
    }

    public class AppFactory
    {
        public App App { get; private set; }

        public AppFactory(string name, string area)
        {
            this.App = new App(name, area);
        }
    }
}