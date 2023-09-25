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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Mindbite.Mox.Configuration.AppMenus
{
    public static class MenuQueryExtensions
    {
        public static AppMenuItem FirstOrDefault(this IEnumerable<AppMenuItem> menu, Func<AppMenuItem, bool> func, bool recursive = true)
        {
            foreach (var item in menu)
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

    public class AppMenuBuilder
    {
        private readonly List<AppMenuItem> _appMenu = new List<AppMenuItem>();

        public AppMenuBuilder Items(Action<AppMenuItemBuilderBuilder> items)
        {
            var builder = new AppMenuItemBuilderBuilder();
            items(builder);

            var builtItems = builder.Build();
            this._appMenu.AddRange(builtItems);

            return this;
        }

        public IEnumerable<MenuItem> Build(IUrlHelper url, IEnumerable<string> roles = null, bool tryMatchingAction = false)
        {
            IEnumerable<MenuItem> build()
            {
                void addItems(MenuItem menuItem, AppMenuItem item)
                {
                    foreach (var _item in item.Items)
                    {
                        if (roles != null && _item.Roles.Any() && !roles.Intersect(_item.Roles).Any())
                            continue;

                        var routeValues = Utils.Dynamics.Merge(new { _item.Area }, _item.RouteValues) as object;
                        var menuItemChild = new MenuItem
                        {
                            Title = _item.Title,
                            Id = _item.Id,
                            CssClass = _item.CssClass,
                            Url = url.Action(_item.Action, _item.Controller, routeValues),
                            Area = _item.Area,
                            Action = _item.Action,
                            Controller = _item.Controller
                        };

                        addItems(menuItemChild, _item);

                        if (menuItemChild.Action != null && menuItemChild.Controller != null && menuItemChild.Area != null)
                        {
                            menuItem.Children.Add(menuItemChild);
                        }
                    }
                }

                foreach (var item in this._appMenu)
                {
                    if (roles != null && item.Roles.Any() && !roles.Intersect(item.Roles).Any())
                        continue;

                    var routeValues = Utils.Dynamics.Merge(new { item.Area }, item.RouteValues) as object;
                    var menuItem = new MenuItem
                    {
                        Title = item.Title,
                        Id = item.Id,
                        CssClass = item.CssClass,
                        Url = url.Action(item.Action, item.Controller, routeValues),
                        Area = item.Area,
                        Action = item.Action,
                        Controller = item.Controller
                    };

                    addItems(menuItem, item);

                    if(menuItem.Action != null && menuItem.Controller != null && menuItem.Area != null)
                    {
                        yield return menuItem;
                    }
                }
            }

            var built = build().ToList();
            built.FixParents();

            var selectedItem = built.Flatten().LastOrDefault(x => x.MatchesView(url.ActionContext, tryMatchingAction));
            if (selectedItem != null)
            {
                selectedItem.Selected = true;
            }

            return built;
        }
    }

    public class AppMenuItem
    {
        public AppMenuItem Parent { get; internal set; }
        public IEnumerable<AppMenuItem> Items { get; internal set; } = Enumerable.Empty<AppMenuItem>();
        public string Title { get; internal set; }
        public string Area { get; internal set; }
        public string CssClass { get; internal set; }
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
                item.Parent = this._menuItem;
            }

            return this;
        }

        public AppMenuItemBuilder Title(string title)
        {
            this._menuItem.Title = title;
            return this;
        }

        public AppMenuItemBuilder CssClass(string cssClass)
        {
            this._menuItem.CssClass = cssClass;
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

        public AppMenuItemBuilder Role(params string[] roles)
        {
            this._menuItem.Roles = this._menuItem.Roles.Concat(roles).ToHashSet();
            return this;
        }
    }

    public class AppMenuEventArgs
    {
        public Apps.App App { get; set; }
        public ViewContext ViewContext { get; set; }
    }
}
