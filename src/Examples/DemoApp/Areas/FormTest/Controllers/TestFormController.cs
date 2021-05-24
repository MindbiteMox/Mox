using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Core.Controllers;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DemoApp.Areas.FormTest.Controllers
{
    [Area(Configuration.MainArea)]
    public class TestFormController : FormController<ViewModels.TestForm>
    {
        public override string ModelTitleFieldName => nameof(ViewModels.TestForm.Name);
        public override string IndexPageHeading => "Test Form";
        public override string ModelDisplayName => "Test";
        public override FormControllerRedirectTarget RedirectAfterSaveTarget => FormControllerRedirectTarget.Self;

        private readonly AppDbContext _context;

        public TestFormController(AppDbContext context)
        {
            this._context = context;
        }

        public override async Task DeleteAsync(Guid id)
        {
            //var item = await this._context.ItemDataModels.FirstAsync(x => x.UID == id);
            //this._context.Remove(item);
            //await this._context.SaveChangesAsync();
        }

        public override Task<IDataTable> GetDataTableAsync(DataTableSort sort)
        {
            //var items = this._context.ItemDataModels;

            //var dataSource = items.Select(x => new
            //{
            //    x.UID,
            //    x.Name
            //});

            var dataTable = DataTableBuilder.Create(Enumerable.Empty<object>().AsQueryable())
                .Page(sort.DataTablePage)
                .Sortable(false);

            return Task.FromResult<IDataTable>(dataTable);
        }

        public override async Task<ViewModels.TestForm> GetViewModelAsync(Guid? id)
        {
            //var item = await this._context.ItemDataModels.FirstOrDefaultAsync(x => x.UID == id);
            //return item != null ? ViewModels.TestForm.From(item) : new ViewModels.TestForm();
            return new ViewModels.TestForm();
        }

        public override async Task<Guid> SaveViewModelAsync(Guid? id, ViewModels.TestForm viewModel)
        {
            //var item = await this._context.ItemViewModels.FirstOrDefaultAsync(x => x.UID == id) ?? new Data.Models.ItemDataModel();

            // TODO: Fill item

            //if (item.UID == id)
            //{
            //    this._context.Update(item);
            //}
            //else
            //{
            //    this._context.Add(item);
            //}

            //await this._context.SaveChangesAsync();

            return Guid.NewGuid();
        }
    }
}
