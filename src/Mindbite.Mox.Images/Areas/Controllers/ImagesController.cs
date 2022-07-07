using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Areas.Controllers
{
    [Area(Configuration.MainArea)]
    public class ImagesController : Controller
    {
        private readonly Data.IImagesDbContext _context;
        private readonly Services.ImageService _imageService;

        public ImagesController(Mox.Services.IDbContextFetcher contextFetcher, Services.ImageService imageService)
        {
            this._context = contextFetcher.FetchDbContext<Data.IImagesDbContext>();
            this._imageService = imageService;
        }

        public async Task<IActionResult> Tools()
        {
            static IQueryable Set(Core.Data.IDbContext context, Type T)
            {
                var method = context.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).First(x => x.Name == nameof(Core.Data.IDbContext.Set) && !x.GetParameters().Any());
                method = method.MakeGenericMethod(T);
                return method.Invoke(context, null) as IQueryable;
            }

            if (Request.Method == "POST")
            {
                try
                {
                    var typeName = Request.Form["Type"].ToString();
                    var type = this._context.Model.FindEntityType(typeName).ClrType;
                    var images = await Set(this._context, type).Cast<Data.Models.Image>().ToListAsync();
                    switch (Request.Form["Button"])
                    {
                        case "CreateMissing":
                            foreach(var image in images)
                            {
                                await this._imageService.CreateMissingSizesAsync(type, image);
                            }
                            break;
                        case "DeleteDepricated":
                            foreach (var image in images)
                            {
                                await this._imageService.DeleteDepricatedSizesAsync(type, image);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    return Content(ex.ToString());
                }
                return RedirectToAction();
            }
            else
            {
                var select = "<select name=\"Type\">";

                var imageTypes = this._context.Model.GetEntityTypes().Where(x => x.ClrType.IsAssignableTo(typeof(Data.Models.Image)));
                foreach(var type in imageTypes)
                {
                    select += $"<option>{type.ClrType.FullName}";
                }

                return Content($"<form method=\"POST\">{select}<input type=\"submit\" name=\"Button\" value=\"CreateMissing\"><input type=\"submit\" name=\"Button\" value=\"DeleteDepricated\">", "text/html");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadMulti(ViewModels.EditorTemplates.MultiImage viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            var imageUIDs = viewModel.Images.ToList();
            var errors = new List<string>();

            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var type = this._context.GetEntityType(viewModel);

                var uploadedImages = await this._imageService.UploadImageAsync(type, file);

                foreach (var uploadedImage in uploadedImages)
                {
                    if(uploadedImage.File != null) 
                    { 
                        imageUIDs.Add(uploadedImage.File.UID);
                    }
                    else
                    {
                        errors.Add($"{uploadedImage.FormFile.FileName}: {uploadedImage.ErrorMessage}");
                    }
                }
            }

            ViewData["Errors"] = errors.AsEnumerable();
            viewModel.Images = imageUIDs.ToArray();

            return View("EditorTemplates/MultiImage", viewModel);
        }

        [HttpPost]
        public IActionResult Remove(Guid id, ViewModels.EditorTemplates.MultiImage viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            viewModel.Images = viewModel.Images.Where(x => x != id).ToArray();

            return View("EditorTemplates/MultiImage", viewModel);
        }

        [HttpPost]
        public IActionResult Move(Guid id, int dir, ViewModels.EditorTemplates.MultiImage viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            try
            {
                var index = viewModel.Images.ToList().IndexOf(id);
                var theList = viewModel.Images.Where((_, i) => i != index).ToList();
                theList.Insert(index + dir, viewModel.Images[index]);
                viewModel.Images = theList.ToArray();
            }
            catch { }

            return View("EditorTemplates/MultiImage", viewModel);
        }
    }
}
