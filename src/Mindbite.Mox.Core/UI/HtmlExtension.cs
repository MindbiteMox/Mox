using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI.Menu;
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

        private IOptions<Configuration.StaticIncludes.IncludeConfig> _includeConfig;
        public IOptions<Configuration.StaticIncludes.IncludeConfig> IncludeConfig => this._includeConfig ?? (this._includeConfig = this._htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<Configuration.StaticIncludes.IncludeConfig>>());

        private IOptions<Configuration.StaticIncludes.StaticFileProviderOptions> _staticFileProviderOptions;
        public IOptions<Configuration.StaticIncludes.StaticFileProviderOptions> StaticFileProviderOptions => this._staticFileProviderOptions ?? (this._staticFileProviderOptions = this._htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<Configuration.StaticIncludes.StaticFileProviderOptions>>());

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

        [Obsolete("Supply menu items directly")]
        public Menu.Renderer.MenuRenderer Menu()
        {
            return new Menu.Renderer.MenuRenderer(this);
        }

        public async Task<IHtmlContent> AppMenuAsync(bool includeApp = false, bool onlyCurrentApp = false, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            return await new Menu.Renderer.MenuRenderer(this).AppMenuAsync(includeApp, onlyCurrentApp, startLevel, maxDepth);
        }

        public IHtmlContent Menu(Action<Configuration.AppMenus.AppMenuItemBuilderBuilder> builderAction, bool selectCurrentMenuByAction = false)
        {
            return new Menu.Renderer.MenuRenderer(this).RenderMenu(builderAction, selectCurrentMenuByAction);
        }

        public IHtmlContent Menu(IEnumerable<MenuItem> root = null, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            return new Menu.Renderer.MenuRenderer(this).RenderMenu(root, startLevel, maxDepth);
        }

        public IHtmlContent Menu(MenuItem root = null, int startLevel = 0, int maxDepth = int.MaxValue)
        {
            return new Menu.Renderer.MenuRenderer(this).RenderMenu(root, startLevel, maxDepth);
        }

        public async Task<IHtmlContent> BreadCrumbsAsync(BreadCrumbMenuReference reference)
        {
            return await new BreadCrumbRenderer(this).RenderAsync(reference);
        }

        public async Task<IHtmlContent> BreadCrumbsAsync(BreadCrumbMenuReference reference, IEnumerable<MenuItem> additionalNodes)
        {
            return await new BreadCrumbRenderer(this).RenderAsync(reference, additionalNodes);
        }

        public async Task<IHtmlContent> BreadCrumbsAsync(BreadCrumbMenuReference reference, Action<Configuration.AppMenus.AppMenuItemBuilderBuilder> builderAction)
        {
            return await new BreadCrumbRenderer(this).RenderAsync(reference, builderAction);
        }

        public IHtmlContent Message()
        {
            return new MessageRenderer(this).Render();
        }

        public async Task<IHtmlContent> DataTableAsync(IDataTable dataTable, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData = null)
        {
            return await new DataTableRenderer(this).RenderAsync(dataTable, viewData);
        }

        public async Task<IHtmlContent> StaticFilesAsync(IEnumerable<Configuration.StaticIncludes.StaticFile> files)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var file in files)
            {
                sb.AppendLine(file.Render(this.IncludeConfig.Value.StaticRoot, this.StaticFileProviderOptions.Value.FileProviders).Value);
            }

            return new HtmlString(sb.ToString());
        }

        public async Task<IHtmlContent> StaticFilesAsync(bool isHtmlHead)
        {
            return await this.HtmlHelper.PartialAsync("Mox/StaticIncludes/_Files", new { RenderInHead = isHtmlHead }.ToExpando());
        }

        public async Task<IHtmlContent> AppStaticFilesAsync(bool isHtmlHead)
        {
            return await this.HtmlHelper.PartialAsync("Mox/StaticIncludes/_AppFiles", new { RenderInHead = isHtmlHead }.ToExpando());
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
