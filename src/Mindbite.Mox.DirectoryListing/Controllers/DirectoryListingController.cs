using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.UI;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IO.Compression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;

namespace Mindbite.Mox.DirectoryListing.Controllers
{
    public class MoxDirectoryListingController<TDocument, TDirectory> : MoxDirectoryListingController<TDocument, TDirectory, ViewModels.Document, ViewModels.DocumentDirectory> where TDocument : Data.Document, new() where TDirectory : Data.DocumentDirectory, new()
    {
        public MoxDirectoryListingController(Mindbite.Mox.Services.IDbContextFetcher context, IWebHostEnvironment webHostEnvironment, Services.DocumentService documentService, IOptions<DocumentServiceOptions> options, IStringLocalizer localizer) : base(context, webHostEnvironment, documentService, options, localizer) { }

        public override ViewModels.Document GetDocumentViewModel(TDocument? document)
        {
            if(document != null)
            {
                return ViewModels.Document.From(document);
            }

            return new ViewModels.Document();
        }

        public override ViewModels.DocumentDirectory GetDirectoryViewModel(TDirectory? directory)
        {
            if(directory != null)
            {
                return ViewModels.DocumentDirectory.From(directory);
            }

            return new ViewModels.DocumentDirectory();
        }
    }

    public abstract class MoxDirectoryListingController<TDocument, TDirectory, TDocumentViewModel, TDirectoryViewModel> : DirectoryListingControllerBase<TDocument, TDirectory> where TDocument : Data.Document, new() where TDirectory : Data.DocumentDirectory, new() where TDocumentViewModel : ViewModels.Document where TDirectoryViewModel : ViewModels.DocumentDirectory
    {
        private readonly Data.IDirectoryListingDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Services.DocumentService _documentService;
        private readonly DocumentServiceOptions _options;
        private readonly IStringLocalizer _localizer;

        public virtual bool RenderHeader => true;
        public virtual bool BreadCrumbsIncludeCurrentMenu => false;
        public virtual string? HeaderPartial => null;
        public virtual bool DefaultBreadCrumbsForDirectoryListing => false;
        public virtual Task<string> GetRootDirectoryName() => Task.FromResult("Toppnivå");
        public virtual Task<IEnumerable<Mindbite.Mox.UI.Menu.MenuItem>> AdditionalBreadCrumbNodesAsync() => Task.FromResult(Enumerable.Empty<Mindbite.Mox.UI.Menu.MenuItem>());

        public abstract TDocumentViewModel GetDocumentViewModel(TDocument? document);
        public abstract TDirectoryViewModel GetDirectoryViewModel(TDirectory? document);

        public MoxDirectoryListingController(Mindbite.Mox.Services.IDbContextFetcher context, IWebHostEnvironment webHostEnvironment, Services.DocumentService documentService, IOptions<DocumentServiceOptions> options, IStringLocalizer localizer) : base(context)
        {
            this._context = context.FetchDbContext<Data.IDirectoryListingDbContext>();
            this._webHostEnvironment = webHostEnvironment;
            this._documentService = documentService;
            this._options = options.Value;
            this._localizer = localizer;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            static Func<T, G> fn<T, G>(Func<T, G> f) => f;

            ViewData["RenderHeader"] = this.RenderHeader;
            ViewData["HeaderPartial"] = this.HeaderPartial;
            ViewData["BreadCrumbsIncludeCurrentMenu"] = this.BreadCrumbsIncludeCurrentMenu;
            ViewData["DefaultBreadCrumbsForDirectoryListing"] = this.DefaultBreadCrumbsForDirectoryListing;
            ViewData["AdditionalBreadCrumbNodes"] = await this.AdditionalBreadCrumbNodesAsync();
            HttpContext.Items["RootDirectoryName"] = await this.GetRootDirectoryName();
            HttpContext.Items["GetDirectories"] = fn((Data.IDirectoryListingDbContext theContext) => this.GetDirectories(theContext));
            await next();
        }

        public async Task<IActionResult> Index(Guid? id, Guid? directoryId, Guid? documentId)
        {
            if (directoryId != null)
            {
                return RedirectToAction("ListDirectory", new { id, directoryId });
            }
            else if (documentId != null)
            {
                var document = await this._context.AllDocumentDirectories.IgnoreQueryFilters().Include(x => x.ParentDirectory).FirstOrDefaultAsync(x => x.UID == documentId);
                return RedirectToAction("ListDirectory", new { id, directoryId = document?.ParentDirectory?.UID });
            }

            return RedirectToAction("ListDirectory", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ListDirectory(Guid? directoryId)
        {
            var dir = await this.GetDirectories()
                .Include(x => x.ChildDirectories)
                .FirstOrDefaultAsync(x => x.UID == directoryId);

            return View("DirectoryListing/ListDirectory", dir);
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid documentId)
        {
            var document = await this.GetDocuments()
                .FirstAsync(x => x.UID == documentId);

            if (!await this._options.AuthorizeDownloadAsync(HttpContext, document))
            {
                return Unauthorized();
            }

            new FileExtensionContentTypeProvider().TryGetContentType(document.FileName, out var contentType);
            return File(document.VirtualFilePath(this._options), contentType ?? "application/octet-stream", document.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DisplayTable(Guid? directoryId, DataTableSort sort, string? filter, bool? global)
        {
            bool globalSearch = global == true;
            var allDirectories = await this.GetDirectories().ToListAsync();

            var directory = await this.GetDirectories().FirstOrDefaultAsync(x => x.UID == directoryId);
            var parentDirectoryId = directory?.Id;
            var directories = await this.GetDirectories().Include(x => x.ParentDirectory).Where(x => x.ParentDirectoryId == parentDirectoryId).ToListAsync();

            if (globalSearch && !string.IsNullOrWhiteSpace(filter))
            {
                directories = allDirectories;
            }

            var directoriesDataSource = directories.Select(x => new
            {
                x.UID,
                x.CreatedOn,
                x.Name,
                IsDirectory = true,
                Extension = "Mapp",
                Icon = "<i class='far fa-folder'></i>",
                PathSort = new HtmlString(x.Name),
                Path = Utils.GetDirectoryPath(allDirectories, x),
                NameSort = sort.DataTableSortDirection == "descending" ? "öööööö" + x.Name : "_____" + x.Name,
            }).ToList();

            var documents = this.GetDocuments().Where(x => x.DirectoryId == parentDirectoryId);

            if (globalSearch && !string.IsNullOrWhiteSpace(filter))
            {
                documents = this.GetDocuments().Include(x => x.Directory);
            }

            var documentsDataSource = (await documents.ToListAsync()).Select(x => new
            {
                x.UID,
                x.CreatedOn,
                x.Name,
                IsDirectory = false,
                x.Extension,
                x.Icon,
                PathSort = new HtmlString(x.Name),
                Path = x.Directory != null ? Utils.GetDirectoryPath(allDirectories, (TDirectory)x.Directory, true) : new List<(Guid, string)>(),
                NameSort = x.Name,
            });

            var dataSource = directoriesDataSource.Concat(documentsDataSource);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                dataSource = dataSource.Where(x => x.Name.ToLower().Contains(filter.ToLower()));
            }

            var dataTable = DataTableBuilder.Create(dataSource.AsQueryable())
                .Sort(x => x.NameSort, SortDirection.Ascending, sort.DataTableSortColumn, sort.DataTableSortDirection)
                .Page(sort.DataTablePage)
                .RowId(x => x.UID)
                .RowLink(x => x.IsDirectory ? Url.Action("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", x.UID } }) : Url.Action("Download", new RouteValueDictionary(RouteData.Values) { { "DocumentId", x.UID } }))
                .EmptyMessage(new HtmlString("Mappen är tom"))
                .Columns(columns =>
                {
                    columns.Add(x => x.NameSort).Title("Namn").Render((_, x) => new HtmlString($"{x.Icon} {x.Name}"));

                    if (globalSearch && !string.IsNullOrWhiteSpace(filter))
                    {
                        columns.Add(x => x.PathSort).Title("Sökväg").CssClass("path-column").Render((_, x) =>
                        {
                            var paths = x.Path.Select(y => $"<a href=\"{Url.Action("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", y.uid } })}\" style=\"display: inline-block; margin: 0\">{y.name}</a>");
                            var breadCrumbs = string.Join(" > ", paths);
                            return new HtmlString(breadCrumbs);
                        });
                    }

                    columns.Add(x => x.Extension).Title("Filtyp").Width(100);
                    columns.Add(x => x.CreatedOn).Title("Uppladdad").Width(150).Render(x => new HtmlString(x.ToShortDateString()));
                })
                .Buttons(buttons =>
                {
                    buttons.EditButton(x => x.IsDirectory ? Url.Action("EditDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", x.UID } }) : Url.Action("EditDocument", new RouteValueDictionary(RouteData.Values) { { "DocumentId", x.UID } }));
                    buttons.DeleteButton(x => x.IsDirectory ? Url.Action("DeleteDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", x.UID } }) : Url.Action("DeleteDocument", new RouteValueDictionary(RouteData.Values) { { "DocumentId", x.UID } }));
                });

            return View("Mox/UI/DataTable", dataTable);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFiles(Guid? directoryId, ViewModels.DocumentUpload viewModel)
        {
            var directory = await GetDirectories()
                .Include(x => x.ChildDirectories)
                .FirstOrDefaultAsync(x => x.UID == directoryId);

            var theDirectoryId = directory?.Id;
            var directoryDocuments = await this.GetDocuments().Where(x => x.DirectoryId == theDirectoryId).ToListAsync();
            var uploadedFileNames = viewModel.UploadedFiles.Select(x => x.FileName);

            if (ModelState.IsValid)
            {
                var newDocuments = await this._documentService.SaveDocumentUploadFormAsync(directory, viewModel, ModelState, directoryDocuments, x => this.DocumentBeforeAddAsync(x), x => this.DocumentAfterAddAsync(x));

                if (ModelState.IsValid)
                {
                    this.DisplayMessage("Dina filer har laddats upp!");
                    return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", directoryId } });
                }
            }

            return View("DirectoryListing/ListDirectory", directory);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDirectory(Guid? directoryId)
        {
            var parentDirectory = await GetDirectories().FirstOrDefaultAsync(x => x.UID == directoryId);

            ViewData["ParentDirectory"] = parentDirectory;

            var viewModel = this.GetDirectoryViewModel(null);
            viewModel.ParentDirectoryId = parentDirectory?.Id;

            return View("DirectoryListing/CreateDirectory", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDirectory(Guid? directoryId, TDirectoryViewModel viewModel)
        {
            var parentDirectory = await GetDirectories().FirstOrDefaultAsync(x => x.UID == directoryId);

            ViewData["ParentDirectory"] = parentDirectory;

            if (ModelState.IsValid)
            {
                if (await this._documentService.DirectoryExistsAsync(viewModel.ParentDirectoryId, viewModel.Name!, null, this.GetDirectories()))
                {
                    ModelState.AddModelError(nameof(viewModel.Name), this._localizer["En mapp med det namnet finns redan"]);
                }
            }

            if (ModelState.IsValid)
            {
                var newDirectory = new TDirectory
                {
                    Name = viewModel.Name ?? string.Empty,
                    ParentDirectoryId = viewModel.ParentDirectoryId
                };

                await this.DirectoryBeforeAddAsync(newDirectory);

                await this._documentService.CreateDirectoryAsync(newDirectory, GetDirectories());

                await this.DirectoryAfterAddAsync(newDirectory);

                this.DisplayMessage("Mappen skapades!");

                return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", newDirectory.UID } });
            }

            return View("DirectoryListing/CreateDirectory", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditDirectory(Guid directoryId)
        {
            var directory = await this.GetDirectories()
                .FirstAsync(x => x.UID == directoryId);

            return View("DirectoryListing/EditDirectory", GetDirectoryViewModel(directory));
        }

        [HttpPost]
        public async Task<IActionResult> EditDirectory(Guid directoryId, TDirectoryViewModel viewModel)
        {
            var directory = await this.GetDirectories().FirstAsync(x => x.UID == directoryId);

            if (ModelState.IsValid)
            {
                if (await this._documentService.DirectoryExistsAsync(viewModel.ParentDirectoryId, viewModel.Name!, directory.Id, this.GetDirectories()))
                {
                    ModelState.AddModelError(nameof(viewModel.Name), this._localizer["En mapp med det namnet finns redan"]);
                }
            }

            if (ModelState.IsValid)
            {
                directory.Name = viewModel.Name ?? string.Empty;
                directory.ParentDirectoryId = viewModel.ParentDirectoryId;

                await this.DirectoryBeforeUpdateAsync(directory);

                await this._documentService.UpdateDirectoryAsync(directory, this.GetDirectories());

                await this.DirectoryAfterUpdateAsync(directory);

                this.DisplayMessage("Ändringarna sparades!");

                return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", directoryId } });
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteDirectory(Guid directoryId)
        {
            var dir = await this.GetDirectories().Include(x => x.ParentDirectory).FirstAsync(x => x.UID == directoryId);

            ViewData["CanDelete"] = true;
            ViewData["RenderDefaultDeleteHeader"] = true;
            ViewData["Layout"] = "Mox/_Layout";
            ViewData["ModelTitleFieldName"] = nameof(Data.DocumentDirectory.Name);
            ViewData["RedirectToIndexRouteValues"] = new { id = RouteData.Values["Id"], directoryId = dir.ParentDirectory?.UID };

            return View("DirectoryListing/Delete", dir);
        }

        [HttpPost]
        [ActionName("DeleteDirectory")]
        public async Task<IActionResult> DoDeleteDirectory(Guid directoryId)
        {
            var directory = await this.GetDirectories()
                .Include(x => x.ParentDirectory)
                .FirstAsync(x => x.UID == directoryId);

            await this._documentService.DeleteDirectoryAsync(directory, this.GetDirectories());

            this.DisplayMessage("Mappen raderades!");

            return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", directory.ParentDirectory?.UID } });
        }

        [HttpGet]
        public async Task<IActionResult> EditDocument(Guid documentId)
        {
            var document = await this.GetDocuments()
                .Include(x => x.Directory)
                .FirstAsync(x => x.UID == documentId);

            ViewData["Directory"] = document.Directory;

            return View("DirectoryListing/EditDocument", GetDocumentViewModel(document));
        }

        [HttpPost]
        public async Task<IActionResult> EditDocument(Guid documentId, ViewModels.Document viewModel)
        {
            static void RenameFile(string filePath, string newFileName)
            {
                var newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName + Path.GetExtension(filePath));
                System.IO.File.Move(filePath, newFilePath);
            }

            var document = await this.GetDocuments()
                .Include(x => x.Directory)
                .FirstAsync(x => x.UID == documentId);

            ViewData["Directory"] = document.Directory;

            if (ModelState.IsValid)
            {
                if (await this._documentService.DocumentExistsAsync(viewModel.DirectoryId, viewModel.Name!, document.Id, this.GetDocuments()))
                {
                    ModelState.AddModelError(nameof(viewModel.Name), this._localizer["En fil med det namnet finns redan"]);
                }
            }

            if (ModelState.IsValid)
            {
                document.DirectoryId = viewModel.DirectoryId;

                await this.DocumentBeforeUpdateAsync(document);

                await this._documentService.UpdateDocumentAsync(document, viewModel.Name!, this.GetDocuments());

                await this.DocumentAfterUpdateAsync(document);

                this.DisplayMessage("Ändringarna sparades!");

                var documentDirectoryUID = await this.GetDirectories().Where(x => x.Id == document.DirectoryId).Select(x => x.UID).FirstOrDefaultAsync();

                return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", documentDirectoryUID } });
            }

            return View("DirectoryListing/EditDocument", viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var document = await this.GetDocuments()
                .Include(x => x.Directory)
                .FirstAsync(x => x.UID == documentId);

            ViewData["CanDelete"] = true;
            ViewData["RenderDefaultDeleteHeader"] = true;
            ViewData["Layout"] = "Mox/_Layout";
            ViewData["ModelTitleFieldName"] = nameof(Data.Document.Name);
            ViewData["RedirectToIndexRouteValues"] = new { Id = RouteData.Values["Id"], directoryId = document.Directory?.UID };

            return View("DirectoryListing/Delete", document);
        }

        [HttpPost]
        [ActionName("DeleteDocument")]
        public async Task<IActionResult> DoDeleteDocument(Guid documentId)
        {
            var document = await this.GetDocuments().FirstAsync(x => x.UID == documentId);
            var documentDirectory = await this.GetDirectories().FirstOrDefaultAsync(x => x.Id == document.DirectoryId);

            await this._documentService.DeleteDocumentAsync(document);

            this.DisplayMessage("Filen raderades!");

            return RedirectToAction("ListDirectory", new RouteValueDictionary(RouteData.Values) { { "DirectoryId", documentDirectory?.UID } });
        }

        [HttpGet]
        public async Task DownloadAll(Guid? directoryId)
        {
            var g = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            g.DisableBuffering();

            var directories = await this.GetDirectories().ToListAsync();
            var documents = await this.GetDocuments().ToListAsync();
            var rootDirectory = directoryId != null ? directories.First(x => x.UID == directoryId) : null;
            var theDirectoryId = rootDirectory?.Id;

            Response.StatusCode = 200;
            Response.ContentType = "application/zip";
            Response.Headers.Add("Content-Disposition", new System.Net.Mime.ContentDisposition
            {
                Inline = false,
                FileName = (rootDirectory?.Name ?? await this.GetRootDirectoryName()) + ".zip"
            }.ToString());

            await this._documentService.WriteDirectoryZipAsync(rootDirectory, Response.BodyWriter.AsStream(), this.GetDirectories(), this.GetDocuments());
        }
    }
}
