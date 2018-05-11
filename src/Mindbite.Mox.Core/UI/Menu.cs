using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.UI.Menus
{
    public interface IMenuItem
    {
        IMenu Menu { get; }
        IMenuItem Parent { get; }
        IEnumerable<IMenuItem> Items { get; }
        string Title { get; }
        string Area { get; }
        string Controller { get; }
        string Action { get; }
        object RouteValues { get; }
        string Id { get; }
        ISet<string> Roles { get; }
    }

    public interface IMenu
    {
        IEnumerable<IMenuItem> Items { get; }
        string Title { get; }
        string Id { get; }
    }

    public static class MenuQueryExtensions
    {
        public static IMenuItem FirstOrDefault(this IMenu menu, Func<IMenuItem, bool> func, bool recursive = true)
        {
            foreach(var item in menu.Items)
            {
                var found = item.FirstOrDefault(func, recursive);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static IMenuItem FirstOrDefault(this IMenuItem menuItem, Func<IMenuItem, bool> func, bool recursive = true)
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

            if(func(menuItem))
            {
                return menuItem;
            }

            return null;
        }

        public static bool MatchesView(this IMenuItem menuItem, ViewContext viewContext, bool tryMatchingAction = false)
        {
            var actionDescriptor = viewContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException($"The view context's action descriptor must be of type {nameof(ControllerActionDescriptor)}");
            }

            var area = viewContext.RouteData.Values["Area"]?.ToString();
            var controller = viewContext.RouteData.Values["Controller"]?.ToString();
            var action = viewContext.RouteData.Values["Action"]?.ToString();

            if ((menuItem.Area == null || menuItem.Area.ToLower() == area.ToLower()) && menuItem.Controller != null && menuItem.Controller.ToLower() == controller.ToLower() && (!tryMatchingAction || menuItem.Action.ToLower() == action.ToLower()))
            {
                return true;
            }

            var selectedMenus = actionDescriptor.ControllerTypeInfo.GetCustomAttributes<SelectMenuAttribute>();

            return selectedMenus.Any(x => x.MenuId.ToLower() == menuItem.Id.ToLower());
        }

        /// <summary>
        /// Ordered by root first
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IMenuItem> Parents(this IMenuItem menuItem)
        {
            var parents = new List<IMenuItem>();

            if (menuItem.Parent != null)
                parents.AddRange(menuItem.Parent.Parents());

            parents.Add(menuItem);

            return parents;
        }

        public static bool CanViewWithRoles(this IMenuItem menuItem, IEnumerable<string> roles)
        {
            if (roles == null)
                return true;
            return roles.Intersect(menuItem.Roles).Count() == menuItem.Roles.Count();
        }
    }

    public class Menu : IMenu
    {
        private List<MenuItem> _menuItems;
        private string _title;
        private string _id;

        IEnumerable<IMenuItem> IMenu.Items => this._menuItems;
        string IMenu.Title => this._title;
        string IMenu.Id => this._id;

        private Menu()
        {
            this._menuItems = new List<MenuItem>();
            this._id = Guid.NewGuid().ToString();
        }

        public Menu Items(Action<MenuItemFactory> items)
        {
            var factory = new MenuItemFactory();
            items(factory);
            this._menuItems.AddRange(factory.Items);

            foreach (var item in factory.Items)
            {
                item.SetMenu(this);
            }

            return this;
        }

        public Menu Title(string title)
        {
            this._title = title;
            return this;
        }

        public Menu Id(string id)
        {
            this._id = id;
            return this;
        }

        public static Menu Create()
        {
            return new Menu();
        }
    }

    public class MenuItem : IMenuItem
    {
        private List<MenuItem> _menuItems;
        private Menu _menu;
        private MenuItem _parent;
        private string _title;
        private string _area;
        private string _controller;
        private string _action;
        private object _routeValues;
        private string _id;
        private HashSet<string> _roles;

        public IMenu Menu => this._menu;
        public IMenuItem Parent => this._parent;
        IEnumerable<IMenuItem> IMenuItem.Items => this._menuItems;
        string IMenuItem.Title => this._title;
        string IMenuItem.Area => this._area;
        string IMenuItem.Controller => this._controller;
        string IMenuItem.Action => this._action;
        object IMenuItem.RouteValues => this._routeValues;
        string IMenuItem.Id => this._id;
        ISet<string> IMenuItem.Roles => this._roles;

        internal MenuItem()
        {
            this._menuItems = new List<MenuItem>();
            this._controller = null;
            this._action = "Index";
            this._id = Guid.NewGuid().ToString();
            this._roles = new HashSet<string>();
        }

        public MenuItem Items(Action<MenuItemFactory> items)
        {
            var factory = new MenuItemFactory();
            items(factory);
            this._menuItems.AddRange(factory.Items);

            foreach(var item in factory.Items)
            {
                item._menu = this._menu;
                item._parent = this;
            }

            return this;
        }

        public MenuItem Title(string title)
        {
            this._title = title;
            return this;
        }

        public MenuItem Action(string controller, string action = "Index", object routeValues = null)
        {
            this._area = null;
            this._controller = controller;
            this._action = action;
            this._routeValues = routeValues;
            return this;
        }

        public MenuItem AreaAction(string area, string controller, string action = "Index", object routeValues = null)
        {
            this._area = area;
            this._controller = controller;
            this._action = action;
            this._routeValues = routeValues;
            return this;
        }

        public MenuItem Id(string id)
        {
            this._id = id;
            return this;
        }

        internal void SetMenu(Menu menu)
        {
            this._menu = menu;
        }

        public MenuItem Role(string role)
        {
            this._roles.Add(role);
            return this;
        }
    }

    public class MenuItemFactory
    {
        private List<MenuItem> _menuItems;

        public IEnumerable<MenuItem> Items => this._menuItems;

        internal MenuItemFactory()
        {
            this._menuItems = new List<MenuItem>();
        }

        public MenuItem Add()
        {
            var newMenuItem = new MenuItem();
            this._menuItems.Add(newMenuItem);
            return newMenuItem;
        }
    }

}
