using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Core.Controllers;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DemoApp.Areas.FormTest.Controllers
{
    [Area(Configuration.MainArea)]
    public class TestFormController : FormController<ViewModels.TestForm, int, int?>
    {
        public override string ModelTitleFieldName => nameof(ViewModels.TestForm.Name);
        public override string IndexPageHeading => "Test Form";
        public override string ModelDisplayName => "Test";
        public override FormControllerRedirectTarget RedirectAfterSaveTarget => FormControllerRedirectTarget.Self;

        private readonly AppDbContext _context;

        public class DataModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
        }

        private static List<DataModel> Data = new List<DataModel>
        {
            new DataModel { Id = 1, Title = "Hello 1" }, 
            new DataModel { Id = 2, Title = "Hello 2" }, 
            new DataModel { Id = 3, Title = "Hello 3" }
        };

        public TestFormController(AppDbContext context)
        {
            this._context = context;
        }

        public override async Task DeleteAsync(int id)
        {
            Data.RemoveAll(x => x.Id == id);
        }

        public override Task<IDataTable> GetDataTableAsync(DataTableSort sort)
        {
            var dataTable = DataTableBuilder.Create(Data.AsQueryable())
                .Page(sort.DataTablePage)
                .EnableSelection(true)
                .RowId(x => x.Id)
                .Sort(x => x.Title, SortDirection.Ascending, sort.DataTableSortColumn, sort.DataTableSortDirection)
                .Columns(columns =>
                {
                    columns.Add(x => x.Id).Title("Id");
                    columns.Add(x => x.Title).Title("Title");
                });

            return Task.FromResult<IDataTable>(dataTable);
        }

        public override async Task<ViewModels.TestForm> GetViewModelAsync(int? id)
        {
            return new ViewModels.TestForm();
        }

        [HttpPost]
        public async Task<IActionResult> RunBulk(ViewModels.TestFormBulkActions bulkAction, DataTableSelection selection)
        {
            switch (bulkAction)
            {
                case ViewModels.TestFormBulkActions.Delete:
                    Data.RemoveAll(x => selection.SelectedIds.Contains(x.Id));
                    break;
            }

            this.DisplayMessage($"{bulkAction.GetDescription()} kördes på {selection.SelectedIds.Length} rader.");
            return RedirectToAction("Index");
        }

        public override async Task<int> SaveViewModelAsync(int? id, ViewModels.TestForm viewModel)
        {
            if(id == null)
            {
                var newId = new Random().Next();
                Data.Add(new DataModel
                {
                    Id = newId,
                    Title = "New"
                });
                return newId;
            }

            return id.Value;
        }
    }
}
