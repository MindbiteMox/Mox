using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mindbite.Mox.UI.Menu;

namespace Mindbite.Mox.Configuration.AppMenus
{
    public static class MenuQueryExtensions
    {
        public static AppMenuItem FirstOrDefault(this AppMenu menu, Func<AppMenuItem, bool> func, bool recursive = true)
        {
            foreach (var item in menu.Items)
            {
                var found = item.FirstOrDefault(func, recursive);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static AppMenuItem FirstOrDefault(this AppMenuItem menuItem, Func<AppMenuItem, bool> func, bool recursive = true)
        {
            if (recursive)
            {
                foreach (var item in menuItem.Items)
                {
                    var found = item.FirstOrDefault(func, recursive);
                    if (found != null)
                        return found;
                }
            }

            if (func(menuItem))
            {
                return menuItem;
            }

            return null;
        }

        /// <summary>
        /// Ordered by root first
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AppMenuItem> Parents(this AppMenuItem menuItem)
        {
            var parents = new List<AppMenuItem>();

            if (menuItem.Parent != null)
                parents.AddRange(menuItem.Parent.Parents());

            parents.Add(menuItem);

            return parents;
        }

        public static bool CanViewWithRoles(this AppMenuItem menuItem, IEnumerable<string> roles)
        {
            if (roles == null)
                return true;
            return roles.Intersect(menuItem.Roles).Count() == menuItem.Roles.Count();
        }
    }

    public class AppMenu
    {
        public IEnumerable<AppMenuItem> Items { get; internal set; } = new List<AppMenuItem>();
        public string Title { get; internal set; }
        public string Id { get; internal set; } = Guid.NewGuid().ToString();

        internal AppMenu()
        {
        }
    }

    public class AppMenuBuilder
    {
        private readonly AppMenu _appMenu = new AppMenu();

        public AppMenuBuilder Items(Action<AppMenuItemBuilderBuilder> items)
        {
            var builder = new AppMenuItemBuilderBuilder();
            items(builder);

            var builtItems = builder.Build();
            this._appMenu.Items = this._appMenu.Items.Concat(builtItems);

            foreach (var item in builtItems)
            {
                item.Menu = this._appMenu;
            }

            return this;
        }

        public AppMenuBuilder Title(string title)
        {
            this._appMenu.Title = title;
            return this;
        }

        public AppMenuBuilder Id(string id)
        {
            this._appMenu.Id = id;
            return this;
        }

        public List<MenuItem> Build(IUrlHelper url, IEnumerable<string> roles = null, ViewContext viewContext = null, bool tryMatchingAction = false)
        {
            IEnumerable<MenuItem> build()
            {
                void addItems(MenuItem menuItem, AppMenuItem item)
                {
                    foreach (var _item in item.Items)
                    {
                        if (roles != null && roles.Any() && item.Roles.Any() && roles.Intersect(item.Roles).Any())
                            continue;

                        var routeValues = Utils.Dynamics.Merge(new { _item.Area }, _item.RouteValues) as object;
                        var menuItemChild = new MenuItem
                        {
                            Title = _item.Title,
                            Id = _item.Id,
                            Url = url.Action(_item.Action, _item.Controller, routeValues),
                            Area = _item.Area,
                            Action = _item.Action,
                            Controller = _item.Controller
                        };
                        menuItem.Children.Add(menuItemChild);
                        addItems(menuItemChild, _item);
                    }
                }

                foreach (var item in this._appMenu.Items)
                {
                    if (roles != null && roles.Any() && item.Roles.Any() && roles.Intersect(item.Roles).Any())
                        continue;

                    var routeValues = Utils.Dynamics.Merge(new { item.Area }, item.RouteValues) as object;
                    var menuItem = new MenuItem
                    {
                        Title = item.Title,
                        Id = item.Id,
                        Url = url.Action(item.Action, item.Controller, routeValues),
                        Area = item.Area,
                        Action = item.Action,
                        Controller = item.Controller
                    };
                    addItems(menuItem, item);

                    yield return menuItem;
                }
            }

            var built = build().ToList();
            built.FixParents();

            if (viewContext != null)
            {
                var selectedItem = built.Flatten().LastOrDefault(x => x.MatchesView(viewContext, tryMatchingAction));
                if (selectedItem != null)
                {
                    selectedItem.Selected = true;
                }
            }

            return built;
        }
    }

    public class AppMenuItem
    {
        public AppMenu Menu { get; internal set; }
        public AppMenuItem Parent { get; internal set; }
        public IEnumerable<AppMenuItem> Items { get; internal set; } = Enumerable.Empty<AppMenuItem>();
        public string Title { get; internal set; }
        public string Area { get; internal set; }
        public string Controller { get; internal set; }
        public string Action { get; internal set; }
        public object RouteValues { get; internal set; }
        public string Id { get; internal set; }
        public ISet<string> Roles { get; internal set; } = new HashSet<string>();

        internal AppMenuItem()
        {
            this.Action = "Index";
            this.Id = Guid.NewGuid().ToString();
        }
    }

    public class AppMenuItemBuilderBuilder
    {
        readonly private List<AppMenuItemBuilder> _builders = new List<AppMenuItemBuilder>();

        internal AppMenuItemBuilderBuilder()
        {
        }

        public AppMenuItemBuilder Add()
        {
            var builder = new AppMenuItemBuilder();
            this._builders.Add(builder);
            return builder;
        }

        internal IEnumerable<AppMenuItem> Build()
        {
            return this._builders.Select(x => x.Build());
        }
    }

    public class AppMenuItemBuilder
    {
        private readonly AppMenuItem _menuItem = new AppMenuItem();

        internal AppMenuItemBuilder()
        {
        }

        internal AppMenuItem Build()
        {
            return this._menuItem;
        }

        public AppMenuItemBuilder Items(Action<AppMenuItemBuilderBuilder> items)
        {
            var builder = new AppMenuItemBuilderBuilder();
            items(builder);

            var builtItems = builder.Build();
            this._menuItem.Items = this._menuItem.Items.Concat(builtItems);

            foreach (var item in builtItems)
            {
                item.Menu = this._menuItem.Menu;
                item.Parent = this._menuItem;
            }

            return this;
        }

        public AppMenuItemBuilder Title(string title)
        {
            this._menuItem.Title = title;
            return this;
        }

        public AppMenuItemBuilder Action(string controller, string action = "Index", object routeValues = null)
        {
            return this.AreaAction(null, controller, action, routeValues);
        }

        public AppMenuItemBuilder AreaAction(string area, string controller, string action = "Index", object routeValues = null)
        {
            this._menuItem.Area = area;
            this._menuItem.Controller = controller;
            this._menuItem.Action = action;
            this._menuItem.RouteValues = routeValues;
            return this;
        }

        public AppMenuItemBuilder Id(string id)
        {
            this._menuItem.Id = id;
            return this;
        }

        public AppMenuItemBuilder Role(string role)
        {
            this._menuItem.Roles = this._menuItem.Roles.Append(role).ToHashSet();
            return this;
        }
    }
}
