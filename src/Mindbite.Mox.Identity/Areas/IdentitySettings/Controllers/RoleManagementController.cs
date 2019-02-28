using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Mindbite.Mox.UI;
using Mindbite.Mox.Services;
using Microsoft.Extensions.Localization;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.Identity.Controllers
{
    [Area(Constants.SettingsArea)]
    [Authorize(Roles = Constants.AdminRole)]
    public class RoleManagementController : Controller
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly IStringLocalizer _localizer;

        public RoleManagementController(IDbContextFetcher dbContextFetcher, IStringLocalizer localizer)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._localizer = localizer;
        }

        [HttpGet]
        public IActionResult Index(DataTableSort sort)
        {
            var dataSource = this._context.Roles.Select(x => new { Name = this._localizer[$"role_{x.Name}"].ToString() });
            var dataTable = DataTableBuilder
                .Create(dataSource)
                .Sort(sort.DataTableSortColumn ?? "Name", sort.DataTableSortDirection ?? "Ascending")
                .Page(sort.DataTablePage)
                .Columns(columns =>
                {
                    columns.Add(x => x.Name).Title(this._localizer["Namn"]);
                });

            return this.ViewOrOk(dataTable);
        }
    }
}
