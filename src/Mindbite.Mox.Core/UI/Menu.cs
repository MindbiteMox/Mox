using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Mindbite.Mox.UI.Menu
{
    public class MenuItem
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public int Depth { get; set; }
        public List<MenuItem> Children { get; set; } = new List<MenuItem>();
        public MenuItem Parent { get; set; }

        private string _Url { get; set; }
        private string _Area { get; set; }
        private string _Action { get; set; }
        private string _Controller { get; set; }
        private bool _Selected { get; set; }

        public IEnumerable<MenuItem> AllParents => this.Parent == null ? new MenuItem[0] : this.Parent.AllParents.Append(this.Parent);

        public string Url
        {
            get => this._Url ?? this.Children.FirstOrDefault()?.Url;
            set => this._Url = value;
        }

        public string Area
        {
            get => this._Area ?? this.Children.FirstOrDefault()?.Area;
            set => this._Area = value;
        }

        public string Controller
        {
            get => this._Controller ?? this.Children.FirstOrDefault()?.Controller;
            set => this._Controller = value;
        }

        public string Action
        {
            get => this._Action ?? this.Children.FirstOrDefault()?.Action;
            set => this._Action = value;
        }

        public bool Selected
        {
            get => this._Selected ? true : Children.Any(x => x.Selected);
            set => this._Selected = value;
        }
    }

    public static class MenuQueryExtensions
    {
        public static bool MatchesView(this MenuItem menuItem, ViewContext viewContext, bool tryMatchingAction = false)
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

        public static IEnumerable<MenuItem> Flatten(this IEnumerable<MenuItem> menu)
        {
            IEnumerable<MenuItem> addChildren(IEnumerable<MenuItem> menuItems)
            {
                foreach (var item in menuItems)
                {
                    yield return item;
                    foreach (var child in addChildren(item.Children))
                    {
                        yield return child;
                    }
                }
            }

            return addChildren(menu);
        }

        public static void FixParents(this List<MenuItem> menu)
        {
            void fixParents(List<MenuItem> parents)
            {
                foreach(var parent in parents)
                {
                    foreach(var child in parent.Children)
                    {
                        child.Parent = parent;
                        fixParents(parent.Children);
                    }
                }
            }

            fixParents(menu);
        }

        public static MenuItem SelectCurrentMenu(this List<MenuItem> menu, ViewContext viewContext, bool tryMatchingAction = false)
        {
            var selectedMenu = menu.Flatten().LastOrDefault(x => x.MatchesView(viewContext, tryMatchingAction));
            if(selectedMenu != null)
            {
                selectedMenu.Selected = true;
            }
            return selectedMenu;
        }
    }
}
