using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Areas.Controllers
{
    [Area(Configuration.MainArea)]
    public class FilesController : Controller
    {
        private readonly Data.IImagesDbContext _context;
        private readonly Services.FileService _fileService;

        public FilesController(Mox.Services.IDbContextFetcher contextFetcher, Services.FileService fileService)
        {
            this._context = contextFetcher.FetchDbContext<Data.IImagesDbContext>();
            this._fileService = fileService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadMulti(ViewModels.EditorTemplates.MultiFile viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            var errors = new List<string>();
            var fileUIDs = viewModel.Files.ToList();

            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var type = this._context.GetEntityType(viewModel);

                var uploadedFile = await this._fileService.UploadFileAsync(type, file);

                if(uploadedFile.File != null)
                {
                    fileUIDs.Add(uploadedFile.File.UID);
                }
                else
                {
                    errors.Add(uploadedFile.ErrorMessage!);
                }
            }

            ViewData["Errors"] = errors.AsEnumerable();
            viewModel.Files = fileUIDs.ToArray();

            return View("EditorTemplates/MultiFile", viewModel);
        }

        [HttpPost]
        public IActionResult Remove(Guid id, ViewModels.EditorTemplates.MultiFile viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            viewModel.Files = viewModel.Files.Where(x => x != id).ToArray();

            return View("EditorTemplates/MultiFile", viewModel);
        }

        [HttpPost]
        public IActionResult Move(Guid id, int dir, ViewModels.EditorTemplates.MultiFile viewModel, string prefix)
        {
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            try
            {
                var index = viewModel.Files.ToList().IndexOf(id);
                var theList = viewModel.Files.Where((_, i) => i != index).ToList();
                theList.Insert(index + dir, viewModel.Files[index]);
                viewModel.Files = theList.ToArray();
            }
            catch { }

            return View("EditorTemplates/MultiFile", viewModel);
        }
    }
}
