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
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Index(DataTableSort sort)
        {
            var dataSource = (await this._context.Roles.ToListAsync()).Select(x => x.SplitIntoLocalizedGroups(this._localizer)).Select(x => new
            {
                Name = x.name,
                Group = string.Join("/", x.groups.Any() ? x.groups : new[] { this._localizer["rolegroup_global"].ToString() })
            });

            var dataTable = DataTableBuilder
                .Create(dataSource.AsQueryable())
                .Sort(sort.DataTableSortColumn ?? "Name", sort.DataTableSortDirection ?? "Ascending")
                .Page(sort.DataTablePage)
                .GroupBy(x => x.Group)
                .Columns(columns =>
                {
                    columns.Add(x => x.Name).Title(this._localizer["Namn"]);
                });

            return this.ViewOrOk(dataTable);
        }
    }
}
