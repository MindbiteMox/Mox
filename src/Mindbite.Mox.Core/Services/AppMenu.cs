using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Services
{
    public class AppMenu
    {
        private readonly MoxHtmlExtensionCollection _htmlExtension;

        public AppMenu(MoxHtmlExtensionCollection moxHtmlExtensionCollection)
        {
            this._htmlExtension = moxHtmlExtensionCollection;
        }

        public UI.MenuBuilder.IMenu GetMenu()
        {
            var roles = this._htmlExtension.GetRolesAsync().Result;
            var builder = UI.MenuBuilder.MenuBuilder.Create();

            void addAppMenuItems(UI.MenuBuilder.MenuBuilder _builder, IEnumerable<UI.Menus.IMenuItem> items)
            {
                foreach(var item in items)
                {
                    _builder.Add(item.Title)
                        .Id(item.Id)
                        .Url(this._htmlExtension.UrlHelper.MenuAction(item, roles, item.RouteValues))
                        .Children(children =>
                        {
                            addAppMenuItems(children, item.Items);
                        });
                }
            }

            void addAppMenu(UI.MenuBuilder.MenuBuilder _builder, UI.Menus.IMenu appMenu)
            {
                _builder.Add(appMenu.Title)
                    .Id(appMenu.Id)
                    .Url(this._htmlExtension.UrlHelper.MenuAction(appMenu, roles))
                    .Children(children =>
                    {
                        addAppMenuItems(children, appMenu.Items);
                    });
            }

            foreach (var app in this._htmlExtension.Config.Value.Apps)
            {
                addAppMenu(builder, app.Menu);
            }

            return builder.Build();
        }
    }
}
