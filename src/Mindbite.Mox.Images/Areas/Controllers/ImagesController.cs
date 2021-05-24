using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [HttpPost]
        public async Task<IActionResult> UploadMulti(ViewModels.EditorTemplates.MultiImage viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            var imageUIDs = viewModel.Images.ToList();

            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var type = this._context.GetEntityType(viewModel);

                var uploadedImages = await this._imageService.UploadImageAsync(type, file);

                foreach (var uploadedImage in uploadedImages)
                {
                    imageUIDs.Add(uploadedImage.UID);
                }
            }

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
