using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.UI.Menu.Renderer
{
    public class MenuRenderer
    {
        private readonly MoxHtmlExtensionCollection _htmlExtensions;

        public MenuRenderer(MoxHtmlExtensionCollection htmlExtensions)
        {
            this._htmlExtensions = htmlExtensions;
        }

        private StringBuilder RenderMenu(StringBuilder sb, IEnumerable<MenuItem> root = null, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            void renderMenuItem(MenuItem item)
            {
                var @class = item.Selected ? "selected" : "";
                @class += item.Selected && item.Children.Any(x => x.Selected) ? " selected-parent" : "";

                sb.Append($"<a href=\"{item.Url}\" class=\"{@class}\">{this._htmlExtensions.Localizer[item.Title]}</a>");
            }

            void TraverseMenuHierarchy(MenuItem item, int depth)
            {
                if (depth >= maxDepth)
                    return;

                if (depth >= startLevel)
                {
                    renderMenuItem(item);

                    if (depth + 1 >= maxDepth)
                        return;

                    if (item.Children.Any())
                    {
                        sb.AppendLine($"<ul class=\"mox-menu\">");
                        foreach (var child in item.Children)
                        {
                            sb.AppendLine($"<li>");
                            TraverseMenuHierarchy(child, depth + 1);
                            sb.AppendLine($"</li>");
                        }
                        sb.AppendLine($"</ul>");
                    }
                }
                else
                {
                    if (depth + 1 >= maxDepth)
                        return;

                    foreach (var child in item.Children)
                    {
                        TraverseMenuHierarchy(child, depth + 1);
                    }
                }
            }

            sb.AppendLine($"<ul class=\"mox-menu\">");
            foreach (var item in root.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
            {
                sb.AppendLine($"<li>");
                TraverseMenuHierarchy(item, 0);
                sb.AppendLine($"</li>");
            }
            sb.AppendLine($"</ul>");

            return sb;
        }

        public async Task<IHtmlContent> AppMenuAsync(bool includeApp = false, bool onlyCurrentApp = false, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            if (!this._htmlExtensions.Config.Value.Apps.Any())
                return HtmlString.Empty;

            var roles = await this._htmlExtensions.GetRolesAsync();

            var menuItems = default(List<MenuItem>);

            if (includeApp)
            {
                menuItems = this._htmlExtensions.Config.Value.Apps.Select(x => new MenuItem
                {
                    Id = x.AppId,
                    Title = x.Name,
                    Children = x.ResolveActiveMenu(this._htmlExtensions.UrlHelper.ActionContext).Build(this._htmlExtensions.UrlHelper, roles).ToList(),
                }).ToList();
                menuItems.FixParents();
            }
            else
            {
                menuItems = this._htmlExtensions.Config.Value.Apps.SelectMany(x => x.ResolveActiveMenu(this._htmlExtensions.UrlHelper.ActionContext).Build(this._htmlExtensions.UrlHelper, roles)).ToList();
                menuItems.FixParents();
            }

            if(onlyCurrentApp)
            {
                var selectedAppMenu = this._htmlExtensions.Config.Value.Apps
                    .Select(x => (app: x, menuItems: x.ResolveActiveMenu(this._htmlExtensions.UrlHelper.ActionContext).Build(this._htmlExtensions.UrlHelper, roles).ToList()))
                    .FirstOrDefault(x => x.menuItems.Flatten().Any(y => y.MatchesView(this._htmlExtensions.UrlHelper.ActionContext)));
                menuItems = selectedAppMenu.menuItems ?? new List<MenuItem>();
                menuItems.FixParents();
            }

            menuItems.SelectCurrentMenu(this._htmlExtensions.HtmlHelper.ViewContext);

            if (this._htmlExtensions.Config.Value.OnAppMenuRendered != null)
            {
                var args = new AppMenuRendererEventArgs
                {
                    MenuItems = menuItems,
                    ViewContext = this._htmlExtensions.HtmlHelper.ViewContext,
                    IncludeApp = includeApp,
                    MaxDepth = maxDepth,
                    OnlyCurrentApp = onlyCurrentApp,
                    StartLevel = startLevel
                };

                this._htmlExtensions.Config.Value.OnAppMenuRendered(args);
                menuItems = args.MenuItems;
            }

            return RenderMenu(menuItems, startLevel, maxDepth);
        }
        
        public IHtmlContent RenderMenu(IEnumerable<MenuItem> root = null, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            var sb = new StringBuilder();
            sb = RenderMenu(sb, root, startLevel, maxDepth);
            return new HtmlString(sb.ToString());
        }

        public IHtmlContent RenderMenu(MenuItem root = null, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            var sb = new StringBuilder();
            sb = RenderMenu(sb, root.Children, startLevel, maxDepth);
            return new HtmlString(sb.ToString());
        }

        public IHtmlContent RenderMenu(Action<Configuration.AppMenus.AppMenuItemBuilderBuilder> builderAction, bool selectCurrentMenuByAction = false)
        {
            var builder = new Configuration.AppMenus.AppMenuBuilder();
            builder.Items(builderAction);

            var items = builder.Build(this._htmlExtensions.UrlHelper).ToList();
            items.SelectCurrentMenu(this._htmlExtensions.HtmlHelper.ViewContext, selectCurrentMenuByAction);
            items.FixParents();
            return RenderMenu(items);
        }
    }

    public class AppMenuRendererEventArgs
    {
        public bool IncludeApp { get; set; }
        public bool OnlyCurrentApp { get; set; }
        public int StartLevel { get; set; }
        public int MaxDepth { get; set; }
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Can be mutated or replaced
        /// </summary>
        public List<MenuItem> MenuItems { get; set; }
    }
}
