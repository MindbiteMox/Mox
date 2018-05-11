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

namespace Mindbite.Mox.DesignDemoApp.Controllers
{
    [Area(Constants.MainArea)]
    public class DesignsController : Controller
    {
        private Data.IDesignDbContext _context;
        private UserManager<MoxUser> _userManager;
        private IHostingEnvironment _hostingEnvironment;
        private INotificationSender _notificationSender;

        public DesignsController(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, IHostingEnvironment hostingEnvironment, INotificationSender notificationSender)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IDesignDbContext>();
            this._userManager = userManager;
            this._hostingEnvironment = hostingEnvironment;
            this._notificationSender = notificationSender;
        }

        public async Task<IActionResult> Index(int? page, string sortColumn, string sortDirection, string filter)
        {
            var dataSource = this._context.Designs.Include(x => x.Images).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                dataSource = dataSource.Where(x => x.Title.ToLower() == filter.ToLower());
            }

            var dataTable = DataTableBuilder
                .Create(dataSource)
                .Sort(sortColumn ?? "Title", sortDirection ?? "Ascending")
                .Page(page)
                .RowLink(x => Url.Action("Display", new { id = x.Id }))
                //.CssClass("mox-datatable clean")
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

            return this.ViewOrOk(dataTable);
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

                var webRoot = this._hostingEnvironment.WebRootPath;
                var fileDirPath = Path.Combine(webRoot, newImage.RelativeDirPath);
                var filePath = Path.Combine(webRoot, newImage.RelativePath);

                Directory.CreateDirectory(fileDirPath);

                using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await model.ImageForm.Image.CopyToAsync(fileStream);
                }

                this._context.Add(newImage);
                await this._context.SaveChangesAsync();

                return RedirectToAction("Images", new { Id = id });
            }

            return View("Images", model);
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

                return RedirectToAction("Display", new { Id = newDesign.Id });
            }

            return View(newDesign);
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

            return View(design);
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

                return RedirectToAction("Display", new { Id = design.Id });
            }

            return View(design);
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
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
        }
    }
}
