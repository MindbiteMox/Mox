using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Mindbite.Mox.Core.Controllers;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI;
using Mindbite.Mox.Identity.ViewModels;
using Mindbite.Mox.Identity;
using Mindbite.Mox.Services;

namespace Mindbite.Mox.Identity.Controllers
{
    [Area(Constants.SettingsArea)]
    public class RoleGroupsController : FormController<RoleGroup, int, int?>
    {
        public override string ModelTitleFieldName => nameof(ViewModels.RoleGroup.GroupName);
        public override string IndexPageHeading => this._localizer["Behörighetsgrupper"];
        public override string ModelDisplayName => this._localizer["Grupp"];

        private readonly Data.MoxIdentityDbContext _context;
        private readonly Services.RoleGroupManager _roleGroupManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer _localizer;

        public RoleGroupsController(IDbContextFetcher dbContextFetcher, Services.RoleGroupManager roleGroupManager, RoleManager<IdentityRole> roleManager, IStringLocalizer localizer)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._roleGroupManager = roleGroupManager;
            this._roleManager = roleManager;
            this._localizer = localizer;
        }

        public override Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override Task<IDataTable> GetDataTableAsync(DataTableSort sort)
        {
            var dataSource = this._context.RoleGroups;

            var dataTable = DataTableBuilder.Create(dataSource)
                .Sort(x => x.GroupName, SortDirection.Ascending, sort.DataTableSortColumn, sort.DataTableSortDirection)
                .Page(sort.DataTablePage)
                .RowLink(x => Url.Action("Edit", new { x.Id }))
                .Columns(columns =>
                {
                    columns.Add(x => x.GroupName).Title(this._localizer["Namn"]);
                })
                .Buttons(buttons =>
                {
                    //buttons.DeleteButton(x => Url.Action("Delete", new { x.Id }));
                });

            return Task.FromResult<IDataTable>(dataTable);
        }

        public override async Task<RoleGroup> GetViewModelAsync(int? id)
        {
            var group = await this._context.RoleGroups
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Id == id);

            var roles = await this._roleManager.Roles.ToListAsync();
            var tree = roles.BuildLocalizedTree(this._localizer);
            var flatTree = tree.Flatten();

            return group != null ? RoleGroup.From(group, flatTree) : new RoleGroup().SetRoles(null, flatTree);
        }

        public override async Task<int> SaveViewModelAsync(int? id, RoleGroup viewModel)
        {
            var group = (await this._roleGroupManager.FindByIdAsync(id ?? 0)) ?? new Data.Models.RoleGroup();

            group.GroupName = viewModel.GroupName;

            if(id == null)
            {
                this._context.Add(group);
            }
            else
            {
                this._context.Update(group);
            }

            await this._context.SaveChangesAsync();

            var selectedRoles = viewModel.RoleTree.Roles.Where(x => x.Checked && !x.IsParent).Select(x => x.Id).ToList();
            await this._roleGroupManager.UpdateAsync(group, selectedRoles);

            return group.Id;
        }
    }
}
