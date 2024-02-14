using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Mindbite.Mox.Attributes;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.Core.Controllers
{
    public enum FormControllerRedirectTarget
    {
        Self,
        Index
    }

    public abstract class FormController<ViewModel_T, Id_T, NullableId_T> : Controller where ViewModel_T : new()
    {
        public virtual string Layout => "Mox/_Layout";
        public virtual string ModelDisplayName => null;
        public virtual string ModelCreatedMessage => this.ModelUpdatedMessage;
        public virtual string ModelDeletedMessage => this.ModelUpdatedMessage;
        public virtual string ModelUpdatedMessage => "Ändringarna sparades!";
        public abstract string ModelTitleFieldName { get; }
        public virtual Func<ViewModel_T, object> RedirectToIndexRouteValues => _ => new object();
        public virtual Func<Id_T, object> RedirectToSelfRouteValues => id => new { id };
        public virtual string EditHeaderPartial { get; }
        public virtual string CreateHeaderPartial { get; }
        public virtual string IndexHeaderPartial { get; }
        public virtual string DeleteHeaderPartial { get; }
        public virtual bool RenderDefaultEditHeader => true;
        public virtual bool RenderDefaultCreateHeader => true;
        public virtual bool RenderDefaultIndexHeader => true;
        public virtual bool RenderDefaultDeleteHeader => true;
        
        public virtual bool CanCreate => true;
        public virtual bool CanEdit => true;
        public virtual bool CanDelete => true;

        public abstract string IndexPageHeading { get; }
        public virtual string EditPageHeading(ViewModel_T viewModel)
        {
            var localizer = HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();

            return typeof(ViewModel_T).GetProperty(this.ModelTitleFieldName).GetValue(viewModel)?.ToString() ?? localizer["Redigera {0}", localizer[this.ModelDisplayName ?? ""]];
        }

        public virtual FormControllerRedirectTarget RedirectAfterSaveTarget => FormControllerRedirectTarget.Index;
        public virtual RedirectToActionResult RedirectAfterSave(Id_T id, ViewModel_T viewModel)
        {
            switch (this.RedirectAfterSaveTarget)
            {
                case FormControllerRedirectTarget.Self:
                    return RedirectToAction("Edit", this.RedirectToSelfRouteValues(id));
                case FormControllerRedirectTarget.Index:
                    return RedirectToAction("Index", this.RedirectToIndexRouteValues(viewModel));
            }

            throw new NotImplementedException();
        }

        public virtual Func<Id_T> GetId => null;

        public virtual Task InitViewDataAsync(string actionName, ViewModel_T viewModel)
        {
            var modelDisplayAttribute = typeof(ViewModel_T).GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault() as DisplayAttribute;
            var localizer = HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();

            ViewData[nameof(Layout)] = this.Layout;
            ViewData[nameof(IndexPageHeading)] = localizer[this.IndexPageHeading];
            ViewData[nameof(ModelDisplayName)] = localizer[this.ModelDisplayName ?? modelDisplayAttribute?.GetName() ?? ""];
            ViewData[nameof(EditPageHeading)] = viewModel != null ? this.EditPageHeading(viewModel) : string.Empty;
            ViewData[nameof(ModelTitleFieldName)] = this.ModelTitleFieldName;
            ViewData[nameof(RedirectToIndexRouteValues)] = viewModel != null ? this.RedirectToIndexRouteValues(viewModel) : new object();
            ViewData[nameof(RenderDefaultCreateHeader)] = this.RenderDefaultCreateHeader;
            ViewData[nameof(RenderDefaultEditHeader)] = this.RenderDefaultEditHeader;
            ViewData[nameof(RenderDefaultIndexHeader)] = this.RenderDefaultIndexHeader;
            ViewData[nameof(RenderDefaultDeleteHeader)] = this.RenderDefaultDeleteHeader;
            ViewData[nameof(CanCreate)] = this.CanCreate;
            ViewData[nameof(CanEdit)] = this.CanEdit;
            ViewData[nameof(CanDelete)] = this.CanDelete;
            ViewData[nameof(CreateHeaderPartial)] = this.CreateHeaderPartial;
            ViewData[nameof(EditHeaderPartial)] = this.EditHeaderPartial;
            ViewData[nameof(IndexHeaderPartial)] = this.IndexHeaderPartial;
            ViewData[nameof(DeleteHeaderPartial)] = this.DeleteHeaderPartial;

            return Task.CompletedTask;
        }

        public virtual void BeforeValidation() { }
        public virtual Task<(bool canDelete, string errorMessage)> CanDeleteAsync(Id_T id, ViewModel_T viewModel) => Task.FromResult((true, default(string)));
        public virtual Task ValidateAsync(NullableId_T id, ViewModel_T viewModel) { return Task.CompletedTask; }
        public virtual Task SetStaticViewModelData(ViewModel_T viewModel) => Task.CompletedTask;

        public abstract Task<ViewModel_T> GetViewModelAsync(NullableId_T id);
        public abstract Task<Id_T> SaveViewModelAsync(NullableId_T id, ViewModel_T viewModel);
        public abstract Task<IDataTable> GetDataTableAsync(DataTableSort sort);
        public abstract Task DeleteAsync(Id_T id);

        [HttpGet]
        public virtual async Task<IActionResult> Index()
        {
            await InitViewDataAsync(nameof(Index), default);
            return View("Form_Index");
        }

        [HttpGet, HttpPost]
        public virtual async Task<IActionResult> Table(DataTableSort sort)
        {
            return (await this.GetDataTableAsync(sort)).GetActionResult(this);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Create()
        {
            if (!CanCreate)
            {
                return Unauthorized();
            }

            var viewModel = await this.GetViewModelAsync(default);
            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Create), viewModel);
            return View("Form_Create", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PreventDuplicateRequests]
        public virtual async Task<IActionResult> Create(ViewModel_T viewModel)
        {
            if (!CanCreate)
            {
                return Unauthorized();
            }

            this.BeforeValidation();
            await this.ValidateAsync(default, viewModel);

            if (ModelState.IsValid)
            {
                var id = await SaveViewModelAsync(default, viewModel);

                if (ModelState.IsValid)
                {
                    this.DisplayMessage(this.ModelCreatedMessage);
                    return RedirectAfterSave(id, viewModel);
                }
            }

            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Create), viewModel);
            return View("Form_Create", viewModel);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(Id_T id)
        {
            if (!CanEdit)
            {
                return Unauthorized();
            }

            var _id = this.GetId != null ? this.GetId() : id;

            var viewModel = await this.GetViewModelAsync((NullableId_T)(object)_id);
            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Edit), viewModel);
            return View("Form_Edit", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Edit(Id_T id, ViewModel_T viewModel)
        {
            if (!CanEdit)
            {
                return Unauthorized();
            }

            var _id = this.GetId != null ? this.GetId() : id;

            this.BeforeValidation();
            await this.ValidateAsync((NullableId_T)(object)_id, viewModel);

            if (ModelState.IsValid)
            {
                var newId = await SaveViewModelAsync((NullableId_T)(object)_id, viewModel);

                if (ModelState.IsValid)
                {
                    this.DisplayMessage(this.ModelUpdatedMessage);
                    return RedirectAfterSave(newId, viewModel);
                }
            }

            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Edit), viewModel);
            return View("Form_Edit", viewModel);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Delete(Id_T id)
        {
            if (!CanDelete)
            {
                return Unauthorized();
            }

            var localizer = HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();

            var _id = this.GetId != null ? this.GetId() : id;

            var viewModel = await this.GetViewModelAsync((NullableId_T)(object)_id);
            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Delete), viewModel);

            var (canDelete, errorMessage) = await this.CanDeleteAsync(_id, viewModel);

            ViewData["CanDelete"] = canDelete;
            ViewData["CanDeleteErrorMessage"] = localizer[errorMessage ?? ""];

            return View("Form_Delete", viewModel);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> DoDelete(Id_T id)
        {
            if (!CanDelete)
            {
                return Unauthorized();
            }

            var _id = this.GetId != null ? this.GetId() : id;
            var viewModel = await this.GetViewModelAsync((NullableId_T)(object)_id);

            var (canDelete, _) = await this.CanDeleteAsync(_id, viewModel);
            if (!canDelete)
            {
                return BadRequest();
            }

            this.BeforeValidation();
            await this.ValidateAsync((NullableId_T)(object)_id, viewModel);

            if (ModelState.IsValid)
            {
                await DeleteAsync(_id);

                if (ModelState.IsValid)
                {
                    this.DisplayMessage(this.ModelDeletedMessage);
                    return RedirectToAction("Index", this.RedirectToIndexRouteValues(viewModel));
                }
            }


            ViewData["CanDelete"] = canDelete;
            ViewData["CanDeleteErrorMessage"] = string.Empty;

            await this.SetStaticViewModelData(viewModel);
            await InitViewDataAsync(nameof(Delete), viewModel);
            return View("Form_Delete", viewModel);
        }
    }

    public abstract class FormController<ViewModel_T> : FormController<ViewModel_T, Guid, Guid?> where ViewModel_T : new()
    {
    }

    public abstract class FormController<ViewModel_T, Id_T> : FormController<ViewModel_T, Id_T, Id_T?> where ViewModel_T : new() where Id_T : struct
    {
    }

    public abstract class SingleEntityFormController<T, DbContext_T> : FormController<T, Guid, Guid?> where T : class, Data.Models.IUIDEntity, new() where DbContext_T : Data.IDbContext
    {
        private readonly Data.IDbContext _context;

        public SingleEntityFormController(Mindbite.Mox.Services.IDbContextFetcher dbContextFetcher)
        {
            this._context = dbContextFetcher.FetchDbContext<DbContext_T>();
        }

        public override async Task DeleteAsync(Guid id)
        {
            var entity = await this._context.Set<T>().FirstAsync(x => x.UID == id);
            this._context.Remove(entity);
            await this._context.SaveChangesAsync();
        }

        public abstract Task SetTableData(DataTableBuilder.QueryableDataTable<T> table, DataTableSort sort);

        public override Task<IDataTable> GetDataTableAsync(DataTableSort sort)
        {
            var data = this._context.Set<T>();
            var table = DataTableBuilder.Create(data)
                .Page(sort.DataTablePage)
                .Buttons(x =>
                {
                    if (this.CanEdit)
                    {
                        x.Add(y => Url.Action("Edit", new { id = y.UID })).CssClass("edit").Title("Redigera");
                    }

                    if (this.CanDelete)
                    {
                        x.Add(y => Url.Action("Delete", new { id = y.UID })).CssClass("delete").Title("Ta bort");
                    }
                });

            if (this.CanEdit)
            {
                table = table.RowLink(y => Url.Action("Edit", new { id = y.UID }));
            }

            this.SetTableData(table, sort);

            return Task.FromResult<IDataTable>(table);
        }

        public async override Task<T> GetViewModelAsync(Guid? id)
        {
            return await this._context.Set<T>().FirstOrDefaultAsync(x => x.UID == id) ?? new T();
        }

        public override void BeforeValidation()
        {
            if (typeof(Data.Models.ISoftDeleted).IsAssignableFrom(typeof(T)))
            {
                var interfaceFields = typeof(Data.Models.ISoftDeleted).GetProperties().Select(x => x.Name);
                foreach (var field in interfaceFields)
                {
                    ModelState.Remove(field);
                }
            }

            if (typeof(Data.Models.IUIDEntity).IsAssignableFrom(typeof(T)))
            {
                var interfaceFields = typeof(Data.Models.IUIDEntity).GetProperties().Select(x => x.Name);
                foreach (var field in interfaceFields)
                {
                    ModelState.Remove(field);
                }
            }
        }

        public async override Task<Guid> SaveViewModelAsync(Guid? id, T viewModel)
        {
            var entity = await this._context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.UID == id);

            if (entity != null)
            {
                viewModel.Id = entity.Id;
                viewModel.UID = entity.UID;

                if (typeof(Data.Models.ISoftDeleted).IsAssignableFrom(typeof(T)))
                {
                    var softDeletedEntity = (Data.Models.ISoftDeleted)entity;
                    var softDeletedViewModel = (Data.Models.ISoftDeleted)viewModel;
                    softDeletedViewModel.CreatedOn = softDeletedEntity.CreatedOn;
                    softDeletedViewModel.CreatedById = softDeletedEntity.CreatedById;
                    softDeletedViewModel.DeletedOn = softDeletedEntity.DeletedOn;
                    softDeletedViewModel.DeletedById = softDeletedEntity.DeletedById;
                    softDeletedViewModel.IsDeleted = softDeletedEntity.IsDeleted;
                }

                this._context.Update(viewModel);
            }
            else
            {
                this._context.Add(viewModel);
            }

            await this._context.SaveChangesAsync();

            return entity.UID;
        }
    }
}
