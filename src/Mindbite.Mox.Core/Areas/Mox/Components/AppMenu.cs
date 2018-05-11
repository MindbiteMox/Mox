using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Mindbite.Mox.Configuration;
using Mindbite.Mox.Services;
using System.Security.Claims;

namespace Mindbite.Mox.ViewComponents
{
    [ViewComponent(Name = "Mox/AppMenu")]
    public class AppMenuComponent : ViewComponent
    {
        private readonly Config _moxConfig;
        private readonly IUserRolesFetcher _rolesFetcher;

        public AppMenuComponent(IOptions<Config> moxConfig, IUserRolesFetcher rolesFetcher)
        {
            this._moxConfig = moxConfig.Value;
            this._rolesFetcher = rolesFetcher;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var area = this.RouteData.Values["Area"];
            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = await this._rolesFetcher.GetRolesAsync(userId);
            var app = this._moxConfig.Apps.Where(x => x.CanViewWithRoles(roles)).SingleOrDefault(x => x.Areas.Contains(area));
            return View(new ViewModels.AppMenuViewModel()
            {
                App = app,
                AppFound = app != null
            });
        }
    }
}