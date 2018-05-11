using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.UI
{
    public class MenuRenderer
    {
        private readonly MoxHtmlExtensionCollection _htmlExtensions;
        public MenuRenderer(MoxHtmlExtensionCollection htmlExtensions)
        {
            this._htmlExtensions = htmlExtensions;
        }

        private async Task<StringBuilder> RenderMenu(StringBuilder sb, IHtmlHelper htmlHelper, IEnumerable<IMenuItem> root = null, int startLevel = 0, int maxDepth = int.MaxValue, bool tryMatchingActions = false)
        {
            var roles = await this._htmlExtensions.GetRolesAsync();

            void RenderMenuItem(Menus.IMenuItem item)
            {
                var selectedMenu = item.FirstOrDefault(x => x.MatchesView(htmlHelper.ViewContext, tryMatchingActions));
                var currentMenuIsSelected = item.FirstOrDefault(x => x == selectedMenu) != null;
                var isParentOfSelectedMenu = currentMenuIsSelected && selectedMenu != null && selectedMenu != item;

                var @class = currentMenuIsSelected ? "selected" : "";
                @class += isParentOfSelectedMenu ? " selected-parent" : "";

                sb.AppendLine($"<a href=\"{this._htmlExtensions.UrlHelper.MenuAction(item, roles)}\" class=\"{@class}\">{this._htmlExtensions.Localizer[item.Title]}</a>");
            }

            void TraverseMenuHierarchy(Menus.IMenuItem item, int depth)
            {
                if (depth >= maxDepth)
                    return;

                if (depth >= startLevel)
                {
                    RenderMenuItem(item);

                    if (depth + 1 >= maxDepth)
                        return;

                    if (item.Items.Any(x => x.CanViewWithRoles(roles)))
                    {
                        sb.AppendLine($"<ul class=\"mox-menu\">");
                        foreach (var child in item.Items.Where(x => x.CanViewWithRoles(roles)))
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

                    foreach (var child in item.Items.Where(x => x.CanViewWithRoles(roles)))
                    {
                        TraverseMenuHierarchy(child, depth + 1);
                    }
                }
            }

            sb.AppendLine($"<ul class=\"mox-menu\">");
            foreach (var item in root.Where(x => x.CanViewWithRoles(roles)))
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
            var sb = new StringBuilder();

            if (onlyCurrentApp)
            {
                var currentApp = this._htmlExtensions.Config.Value.Apps.FirstOrDefault(x => x.Areas.Contains(this._htmlExtensions.HtmlHelper.ViewContext.RouteData.Values["Area"]));
                if (includeApp)
                {
                    sb.AppendLine($"<ul class=\"mox-menu\">");
                    sb.AppendLine($"<li>");
                    sb.AppendLine($"<a href=\"{this._htmlExtensions.UrlHelper.AppAction(currentApp, roles)}\" class=\"selected selected-parent app\">{this._htmlExtensions.Localizer[currentApp.Name]}</a>");

                    sb = await RenderMenu(sb, this._htmlExtensions.HtmlHelper, ((IMenu)currentApp.Menu).Items, startLevel, maxDepth - 1, false);

                    sb.AppendLine($"</li>");
                    sb.AppendLine($"</ul>");
                }
                else
                {
                    sb = await RenderMenu(sb, this._htmlExtensions.HtmlHelper, ((IMenu)currentApp.Menu).Items, startLevel, maxDepth, false);
                }
            }
            else
            {
                if (includeApp)
                {
                    var currentApp = this._htmlExtensions.Config.Value.Apps.FirstOrDefault(x => x.Areas.Contains(this._htmlExtensions.HtmlHelper.ViewContext.RouteData.Values["Area"]));
                    sb.AppendLine($"<ul class=\"mox-menu\">");
                    foreach (var app in this._htmlExtensions.Config.Value.Apps.Where(x => x.CanViewWithRoles(roles) && ((IMenu)x.Menu).Items.Any(y => y.CanViewWithRoles(roles) && (!y.Items.Any() || y.Items.Any(z => z.CanViewWithRoles(roles))))))
                    {
                        var @class = app == currentApp ? "selected selected-parent app" : "app";

                        sb.AppendLine($"<li>");
                        sb.AppendLine($"<a href=\"{this._htmlExtensions.UrlHelper.AppAction(app, roles)}\" class=\"{@class}\">{this._htmlExtensions.Localizer[app.Name]}</a>");

                        sb = await RenderMenu(sb, this._htmlExtensions.HtmlHelper, ((IMenu)app.Menu).Items, startLevel, maxDepth - 1, false);

                        sb.AppendLine($"</li>");
                    }
                    sb.AppendLine($"</ul>");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return new HtmlString(sb.ToString());
        }

        public async Task<IHtmlContent> MenuAsync(IMenu root = null, int startLevel = 0, int maxDepth = int.MaxValue, bool tryMatchingActions = false)
        {
            var sb = new StringBuilder();
            sb = await RenderMenu(sb, this._htmlExtensions.HtmlHelper, root.Items, startLevel, maxDepth, tryMatchingActions);
            return new HtmlString(sb.ToString());
        }

        public async Task<IHtmlContent> MenuAsync(IMenuItem root = null, int startLevel = 0, int maxDepth = int.MaxValue, bool tryMatchingActions = false)
        {
            var sb = new StringBuilder();
            sb = await RenderMenu(sb, this._htmlExtensions.HtmlHelper, root.Items, startLevel, maxDepth, tryMatchingActions);
            return new HtmlString(sb.ToString());
        }
    }
}
