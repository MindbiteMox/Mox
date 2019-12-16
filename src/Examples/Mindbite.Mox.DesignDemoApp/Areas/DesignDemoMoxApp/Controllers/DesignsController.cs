using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox;
using Mindbite.Mox.DesignDemoApp.Configuration;
using Mindbite.Mox.DesignDemoApp.Data.Models;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Notifications;
using Mindbite.Mox.Services;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Mindbite.Mox.DesignDemoApp.Controllers
{
    [Area(Constants.MainArea)]
    public class DesignsController : Controller
    {
        private readonly Data.IDesignDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationSender _notificationSender;

        public DesignsController(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, IWebHostEnvironment webHostEnvironment, INotificationSender notificationSender)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IDesignDbContext>();
            this._userManager = userManager;
            this._webHostEnvironment = webHostEnvironment;
            this._notificationSender = notificationSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Table(UI.DataTableSort tableSort)
        {
            var dataSource = this._context.Designs.Include(x => x.Images).AsQueryable();

            var dataTable = DataTableBuilder
                .Create(dataSource)
                .Sort(x => x.Title, SortDirection.Ascending, tableSort.DataTableSortColumn, tableSort.DataTableSortDirection)
                .Page(tableSort.DataTablePage)
                .RowLink(x => Url.Action("Display", new { id = x.Id }))
                .Columns(columns =>
                {
                    columns.Add(x => x.Id).Title("Id").Width(100);
                    columns.Add(x => x.Title).Title("Titel");
                    columns.Add(x => x.Images.Count).Title("Bilder").Width(100);
                })
                .Buttons(buttons =>
                {
                    buttons.Add(x => Url.Action("display", new { Id = x.Id })).CssClass("display");
                    buttons.Add(x => Url.Action("edit", new { Id = x.Id })).CssClass("edit");
                    buttons.Add(x => Url.Action("delete", new { Id = x.Id })).CssClass("delete");
                });

            return this.PartialView("Mox/UI/DataTable", dataTable);
        }

        [HttpGet]
        public async Task<IActionResult> Display(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var design = await this._context.Designs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (design == null)
            {
                return NotFound();
            }

            return View(design);
        }

        [HttpGet]
        public async Task<IActionResult> Images(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var design = await this._context.Designs.Include(x => x.Images).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (design == null)
            {
                return NotFound();
            }

            return View(new ViewModels.ImagesViewModel()
            {
                Design = design
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Images")]
        public async Task<IActionResult> UploadImage(int? id, ViewModels.ImagesViewModel model)
        {
            if (id == null)
            {
                return NotFound();
            }

            var design = await this._context.Designs.Include(x => x.Images).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (design == null)
            {
                return NotFound();
            }

            model.Design = design;

            if(ModelState.IsValid)
            {
                var newImage = new Image()
                {
                    DesignId = design.Id,
                    UID = Guid.NewGuid(),
                    Title = model.ImageForm.Title,
                };

                var webRoot = this._webHostEnvironment.WebRootPath;
                var fileDirPath = Path.Combine(webRoot, newImage.RelativeDirPath);
                var filePath = Path.Combine(webRoot, newImage.RelativePath);

                Directory.CreateDirectory(fileDirPath);

                using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await model.ImageForm.Image.CopyToAsync(fileStream);
                }

                this._context.Add(newImage);
                await this._context.SaveChangesAsync();

                return AjaxRedirectToAction("Images", new { Id = id });
            }

            return AjaxView("Images", model);
        }

        public class AsyncJsonResult : JsonResult
        {
            public AsyncJsonResult(object value) : base(value)
            {
            }

            public AsyncJsonResult(object value, JsonSerializerSettings serializerSettings) : base(value, serializerSettings)
            {

            }

            public override async Task ExecuteResultAsync(ActionContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if(this.Value is Func<Task<object>>)
                {
                    this.Value = await ((Func<Task<object>>)this.Value)();
                }
                
                await base.ExecuteResultAsync(context);
            }
        }

        public IActionResult AjaxView(string viewName = null, object model = null)
        {
            if(this.Request.IsAjaxRequest())
            {
                var viewRenderer = HttpContext.RequestServices.GetService<IViewRenderService>();
                Func<Task<object>> jsonAction = async () => new {
                    action = "replaceWithContent",
                    data = await viewRenderer.RenderToStringAsync(this.ControllerContext, viewName, model),
                    model
                };
                return new AsyncJsonResult(jsonAction);
            }
            else
            {
                if(viewName != null && model != null) 
                {
                    return View(viewName, model);
                } 
                else if(viewName != null)
                {
                    return View(viewName);
                }
                else if(model != null)
                {
                    return View(model);
                }
                return View();
            }
        }

        public IActionResult AjaxRedirectToAction(string actionName, object routeValues = null, object model = null)
        {
            if(this.Request.IsAjaxRequest())
            {
                return Json(new 
                {
                    action = "redirect",
                    data = Url.Action(actionName, routeValues),
                    routeValues,
                    model
                });
            }
            else
            {
                return RedirectToAction(actionName, routeValues);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, Title, Description")] Design newDesign)
        {
            if (ModelState.IsValid)
            {
                this._context.Add(newDesign);
                await this._context.SaveChangesAsync();

                return AjaxRedirectToAction("Index", model: new {
                    newDesign.Id
                });
            }

            return AjaxView(model: newDesign);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var design = await this._context.Designs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if(design == null)
            {
                return NotFound();
            }

            return View(model: design);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("Id, Title, Description")] Design design)
        {
            if(id != design.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                this._context.Update(design);
                await this._context.SaveChangesAsync();

                var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                var url = Url.Action("Display", "Designs", new { Area = Constants.MainArea, Id = id }, Request.Scheme);
                await this._notificationSender.SendAsync(me, "Mox.Designs.Edit", shortDescription: "Ny design ändrades!", url: url, entityId: id);

                return AjaxRedirectToAction("Index");
            }

            return AjaxView(model: design);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var design = await this._context.Designs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (design == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] = "Kunde inte ta bort. Försök igen";
            }

            return View(design);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designToDelete = await this._context.Designs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (designToDelete == null)
            {
                return NotFound();
            }

            try
            {
                this._context.Remove(designToDelete);
                await this._context.SaveChangesAsync();
                return AjaxRedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                return AjaxRedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
        }
    }
}
