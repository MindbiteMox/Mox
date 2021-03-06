﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.UI
{
    public class MoxHtmlExtensionCollection
    {
        private readonly IHtmlHelper _htmlHelper;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public IHtmlHelper HtmlHelper => _htmlHelper;

        private IUrlHelper _urlHelper;
        public IUrlHelper UrlHelper => this._urlHelper ?? (this._urlHelper = this._urlHelperFactory.GetUrlHelper(this._htmlHelper.ViewContext));

        private IOptions<Configuration.Config> _config;
        public IOptions<Configuration.Config> Config => this._config ?? (this._config = (IOptions<Configuration.Config>)this._htmlHelper.ViewContext.HttpContext.RequestServices.GetService(typeof(IOptions<Configuration.Config>)));

        private Services.IUserRolesFetcher _rolesFetcher;
        public Services.IUserRolesFetcher RolesFetcher => this._rolesFetcher ?? (this._rolesFetcher = (Services.IUserRolesFetcher)this._htmlHelper.ViewContext.HttpContext.RequestServices.GetService(typeof(Services.IUserRolesFetcher)));

        private string _userId;
        public string UserId => this._userId ?? (this._userId = this._htmlHelper.ViewContext.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        private ISet<string> _roles;
        public async Task<ISet<string>> GetRolesAsync()
        {
            if(this._roles == null)
                this._roles = await RolesFetcher.GetRolesAsync(this.UserId);
            return this._roles; 
        }

        private IStringLocalizer _localizer;
        public IStringLocalizer Localizer => this._localizer ?? (this._localizer = (IStringLocalizer)this._htmlHelper.ViewContext.HttpContext.RequestServices.GetService(typeof(IStringLocalizer)));

        public MoxHtmlExtensionCollection(IHtmlHelper htmlHelper, IUrlHelperFactory urlHelperFactory)
        {
            this._htmlHelper = htmlHelper;
            this._urlHelperFactory = urlHelperFactory;
        }

        public Menu.Renderer.MenuRenderer Menu()
        {
            return new Menu.Renderer.MenuRenderer(this);
        }
    }

    public static class HtmlExtension
    {
        public static MoxHtmlExtensionCollection Mox(this IHtmlHelper htmlHelper)
        {
            var urlHelperFactory = htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            return htmlHelper.ViewBag.MoxHtmlExtensionCollection as MoxHtmlExtensionCollection ?? (htmlHelper.ViewBag.MoxHtmlExtensionCollection = new MoxHtmlExtensionCollection(htmlHelper, urlHelperFactory));
        }
    }
}
