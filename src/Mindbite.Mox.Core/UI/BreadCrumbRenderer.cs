using Microsoft.AspNetCore.Html;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Mindbite.Mox.UI
{
    public enum BreadCrumbMenuReference
    {
        /// <summary>
        /// The breadcrumb is empty and will only show the nodes you supply yourself
        /// </summary>
        Root,
        /// <summary>
        /// The breadcrumb will every menu node except the most specific one you are currently at
        /// </summary>
        Parent,
        /// <summary>
        /// The breadcrumb will be complete with the full path from the current selected menu node
        /// </summary>
        Current
    }

    public class BreadCrumbRenderer
    {
        private readonly MoxHtmlExtensionCollection _htmlExtensions;

        public BreadCrumbRenderer(MoxHtmlExtensionCollection htmlExtensions)
        {
            this._htmlExtensions = htmlExtensions;
        }

        private void Render(StringBuilder sb, IEnumerable<MenuItem> nodes, IEnumerable<string> roles)
        {
            sb.AppendLine("<ul class=\"mox-breadcrumbs\">");
            
            foreach(var node in nodes)
            {
                sb.AppendLine($"<li><a href=\"{HttpUtility.HtmlEncode(this._htmlExtensions.UrlHelper.MenuAction(node, roles))}\">{HttpUtility.HtmlEncode(this._htmlExtensions.Localizer[node.Title])}</a></li>");
            }

            sb.AppendLine("</ul>");
        }

        public async Task<IHtmlContent> RenderAsync(BreadCrumbMenuReference reference, IEnumerable<MenuItem> additionalNodes)
        {
            var nodes = new List<MenuItem>();
            var sb = new StringBuilder();

            if (reference == BreadCrumbMenuReference.Root)
            {
                nodes.AddRange(additionalNodes);

                this.Render(sb, nodes, Enumerable.Empty<string>());
            }
            else
            {
                var roles = await this._htmlExtensions.GetRolesAsync();
                var selectedAppMenu = _htmlExtensions.Config.Value.Apps
                    .Select(x => (app: x, menuItem: x.ResolveActiveMenu(this._htmlExtensions.HtmlHelper.ViewContext).Build(this._htmlExtensions.UrlHelper, roles).Flatten().LastOrDefault(y => y.MatchesView(this._htmlExtensions.HtmlHelper.ViewContext))))
                    .FirstOrDefault(x => x.menuItem != null);

                if(selectedAppMenu.app == null || selectedAppMenu.menuItem == null)
                {
                    throw new Exception($"Could not render bread crumbs because no menu is selected!\nWhen using BreadCrumbMenuReference.{BreadCrumbMenuReference.Parent} or BreadCrumbMenuReference.{BreadCrumbMenuReference.Current} a menu must be selected.\n\nCheck your menus or put the [SelectMenu(\"<menu id>\")] attribute on your controller when it doesn't belong in the menu.");
                }

                var currentApp = selectedAppMenu.app;
                var selectedMenu = selectedAppMenu.menuItem;

                nodes.Add(new MenuItem { Url = this._htmlExtensions.UrlHelper.AppAction(currentApp, roles), Title = currentApp.Name });

                if (reference == BreadCrumbMenuReference.Current)
                {
                    nodes.AddRange(selectedMenu.AllParents);
                    nodes.Add(selectedMenu);
                    nodes.AddRange(additionalNodes);
                }
                else if(reference == BreadCrumbMenuReference.Parent)
                {
                    nodes.AddRange(selectedMenu.AllParents);
                    nodes.Add(selectedMenu);
                    nodes.AddRange(additionalNodes);
                }

                this.Render(sb, nodes, roles);
            }

            return new HtmlString(sb.ToString());
        }

        public async Task<IHtmlContent> RenderAsync(BreadCrumbMenuReference reference, Action<Configuration.AppMenus.AppMenuItemBuilderBuilder> builderAction)
        {
            var builder = new Configuration.AppMenus.AppMenuBuilder();
            builder.Items(builderAction);

            var roles = await this._htmlExtensions.GetRolesAsync();

            return await this.RenderAsync(reference, builder.Build(_htmlExtensions.UrlHelper, roles));
        }

        public async Task<IHtmlContent> RenderAsync(BreadCrumbMenuReference reference)
        {
            return await this.RenderAsync(reference, Enumerable.Empty<MenuItem>());
        }
    }
}
