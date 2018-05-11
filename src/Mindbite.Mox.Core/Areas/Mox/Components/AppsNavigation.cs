using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Mindbite.Mox.Configuration;
using Mindbite.Mox.Attributes;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Mindbite.Mox.Services;
using System.Security.Claims;

namespace Mindbite.Mox.ViewComponents
{
    [ViewComponent(Name = "Mox/AppsNavigation")]
    public class AppsNavigationViewComponent : ViewComponent
    {
        private readonly Config _moxConfig;
        private readonly IUserRolesFetcher _rolesFetcher;

        public AppsNavigationViewComponent(IOptions<Config> moxConfig, IUserRolesFetcher rolesFetcher)
        {
            this._moxConfig = moxConfig.Value;
            this._rolesFetcher = rolesFetcher;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = await this._rolesFetcher.GetRolesAsync(userId);
            return View(new ViewModels.AppsNavigationViewModel(this, _moxConfig, roles));
        }
    }
}