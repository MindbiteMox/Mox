using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mindbite.Mox.DirectoryListing.Services
{
    public class DocumentService
    {
        private readonly Data.IDirectoryListingDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger _logger;
        private readonly DocumentServiceOptions _options;

        public DocumentService(Mindbite.Mox.Services.IDbContextFetcher context, IWebHostEnvironment env, ILogger<DocumentService> logger, IOptions<DocumentServiceOptions> options)
        {
            this._context = context.FetchDbContext<Data.IDirectoryListingDbContext>();
            this._env = env;
            this._logger = logger;
            this._options = options.Value;
        }

        public async Task<IEnumerable<Data.Document>> SaveDocumentUploadFormAsync<TDocument, TDirectory>(TDirectory? directory, ViewModels.DocumentUpload viewModel, ModelStateDictionary modelState, IEnumerable<TDocument>? allDirectoryDocuments = null, Func<TDocument, Task>? beforeAdd = null, Func<TDocument, Task>? afterAdd = null) where TDocument : Data.Document, new() where TDirectory : Data.DocumentDirectory, new()
        {
            var directoryId = directory?.Id;
            allDirectoryDocuments ??= await this._context.Set<TDocument>().Where(x => x.DirectoryId == directoryId).ToListAsync();

            var newDocuments = new List<Data.Document>();

            foreach (var file in viewModel.UploadedFiles)
            {
                try
                {
                    newDocuments.Add(await this.UploadFileAsync(directory, Utils.RemoveInvalidFileNameChars(file.FileName), file.OpenReadStream(), beforeAdd, afterAdd));
                }
                catch (Exception ex)
                {
                    if (this._env.IsDevelopment())
                    {
                        throw;
                    }
                    else
                    {
                        this._logger.LogError(ex.ToString());
                    }
                    modelState.AddModelError("", $"\"{file.FileName}\" kunde inte laddas upp! {ex}");
                }
            }

            var uploadedFileNames = viewModel.UploadedFiles.Select(x => x.FileName);

            foreach (var duplicate in uploadedFileNames.Where(x => allDirectoryDocuments.Any(y => x == y.FileName)))
            {
                var theNewDocument = newDocuments.FirstOrDefault(x => x.FileName == duplicate);
                if (theNewDocument == null)
                {
                    continue;
                }

                DoDuplicateAction(theNewDocument, allDirectoryDocuments, viewModel.OverwriteFiles);
            }

            await this._context.SaveChangesAsync();

            return newDocuments;
        }

        private void DoDuplicateAction<TDocument>(TDocument newDocument, IEnumerable<TDocument> allDirectoryDocuments, ViewModels.DuplicateAction duplicateAction) where TDocument : Data.Document
        {
            switch (duplicateAction)
            {
                case ViewModels.DuplicateAction.Overwrite:
                    {
                        var original = allDirectoryDocuments.FirstOrDefault(x => x.FileName == newDocument.FileName);
                        if (original != null)
                        {
                            this._context.Remove(original);

                            newDocument.CreatedOn = original.CreatedOn;
                            this._context.Update(newDocument);
                        }
                    }
                    break;
                case ViewModels.DuplicateAction.KeepBoth:
                    {
                        var maxUniqueNumber = allDirectoryDocuments.Select(x => x.FileName).Where(x => x.StartsWith(newDocument.FileName)).Select(x => Regex.Replace(x, @".*\(\d+\)\.?.*$", "$1").TryToInt() ?? 0).DefaultIfEmpty().Max();

                        newDocument.FileName = $"{Path.GetFileNameWithoutExtension(newDocument.FileName)} ({maxUniqueNumber + 1}){Path.GetExtension(newDocument.FileName)}";
                        newDocument.Name = Path.GetFileNameWithoutExtension(newDocument.FileName);
                        this._context.Update(newDocument);
                    }
                    break;
                case ViewModels.DuplicateAction.Ignore:
                    {
                        this._context.Remove(newDocument);
                    }
                    break;
            }
        }

        private async Task<Data.Document> UploadFileAsync<TDocument, TDirectory>(TDirectory? directory, string fileName, Stream dataStream, Func<TDocument, Task>? beforeAdd = null, Func<TDocument, Task>? afterAdd = null) where TDocument : Data.Document, new() where TDirectory : Data.DocumentDirectory, new()
        {
            using var transaction = await this._context.Database.BeginTransactionAsync();

            var cleanFileName = Utils.RemoveInvalidFileNameChars(fileName);

            var newDocument = new TDocument
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                FileName = cleanFileName,
                DirectoryId = directory?.Id
            };

            if(beforeAdd != null)
            {
                await beforeAdd(newDocument);
            }

            this._context.Add(newDocument);
            await this._context.SaveChangesAsync();

            if(afterAdd != null)
            {
                await afterAdd(newDocument);
            }

            var filePath = newDocument.FilePath(this._options, this._env);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dataStream.CopyToAsync(fileStream);
            }

            await transaction.CommitAsync();

            return newDocument;
        }

        public void RenameDocument(Data.Document document, string newFileName)
        {
            newFileName = Utils.RemoveInvalidFileNameChars(newFileName);

            if (document.Name != newFileName)
            {
                var filePath = document.FilePath(this._options, this._env);
                var newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName + Path.GetExtension(filePath));
                System.IO.File.Move(filePath, newFilePath);

                document.FileName = newFileName + Path.GetExtension(filePath);
                document.Name = newFileName;
            }
        }

        public async Task DeleteDirectoryAsync<TDirectory>(TDirectory directory, IQueryable<TDirectory>? allDirectoriesQueryable = null) where TDirectory : Data.DocumentDirectory
        {
            static async Task DeleteDirectoryRec(Data.IDirectoryListingDbContext context, IQueryable<TDirectory> directories, int id)
            {
                var dir = await directories
                    .Include(x => x.Documents)
                    .Include(x => x.ChildDirectories)
                    .FirstAsync(x => x.Id == id);

                foreach (var document in dir.Documents)
                {
                    context.Remove(document);
                }

                foreach (var subDir in dir.ChildDirectories)
                {
                    await DeleteDirectoryRec(context, directories, subDir.Id);
                }

                context.Remove(dir);
                await context.SaveChangesAsync();
            }

            await DeleteDirectoryRec(_context, allDirectoriesQueryable ?? this._context.Set<TDirectory>(), directory.Id);
        }

        public async Task DeleteDocumentAsync<TDocument>(TDocument document) where TDocument : Data.Document
        {
            this._context.Remove(document);
            await this._context.SaveChangesAsync();
        }

        public async Task WriteDirectoryZipAsync<TDocument, TDirectory>(TDirectory? directory, Stream outputStream, IQueryable<TDirectory>? allDirectoriesQueryable = null, IQueryable<TDocument>? allDocumentsQueryable = null) where TDocument : Data.Document where TDirectory : Data.DocumentDirectory
        {
            var directories = (await (allDirectoriesQueryable ?? this._context.Set<TDirectory>()).ToListAsync()).ToLookup(x => x.ParentDirectoryId);
            var documents = (await (allDocumentsQueryable ?? this._context.Set<TDocument>()).ToListAsync()).ToLookup(x => x.DirectoryId);

            using var zip = new ZipArchive(outputStream, ZipArchiveMode.Create);

            async Task addDirectoryAsync(int? parentDirectoryId, string path)
            {
                foreach (var child in directories[parentDirectoryId])
                {
                    await addDirectoryAsync(child.Id, $"{path}{child.Name}/");
                }

                foreach (var document in documents[parentDirectoryId])
                {
                    var filePath = document.FilePath(this._options, this._env);
                    if (File.Exists(filePath))
                    {
                        var entry = zip.CreateEntry($"{path}{document.FileName}");
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        using var es = entry.Open();
                        await fs.CopyToAsync(es);
                    }
                }
            }

            await addDirectoryAsync(directory?.Id, "");
        }

        public async Task<bool> DirectoryExistsAsync<TDirectory>(int? parentId, string name, int? excludeDirectoryId, IQueryable<TDirectory>? allDirectoriesQueryable = null) where TDirectory : Data.DocumentDirectory
        {
            return await (allDirectoriesQueryable ?? this._context.Set<TDirectory>()).AnyAsync(x => x.ParentDirectoryId == parentId && x.Name == name && (excludeDirectoryId == null || x.Id != excludeDirectoryId));
        }

        public async Task<bool> DocumentExistsAsync<TDocument>(int? directoryId, string name, int? excludeDocumentId, IQueryable<TDocument>? allDocumentsQueryable = null) where TDocument : Data.Document
        {
            var cleanFileName = Utils.RemoveInvalidFileNameChars(name);

            return await (allDocumentsQueryable ?? this._context.Set<TDocument>()).AnyAsync(x => x.DirectoryId == directoryId && x.Name == cleanFileName && (excludeDocumentId == null || x.Id == excludeDocumentId));
        }

        public async Task CreateDirectoryAsync<TDirectory>(TDirectory directory, IQueryable<TDirectory>? allDirectoriesQueryable = null) where TDirectory : Data.DocumentDirectory
        {
            if(await this.DirectoryExistsAsync(directory.ParentDirectoryId, directory.Name, null, allDirectoriesQueryable))
            {
                throw new DirectoryListingException("Directory already exists");
            }

            this._context.Add(directory);
            await this._context.SaveChangesAsync();
        }

        public async Task UpdateDirectoryAsync<TDirectory>(TDirectory directory, IQueryable<TDirectory>? allDirectoriesQueryable = null) where TDirectory : Data.DocumentDirectory
        {
            if (await this.DirectoryExistsAsync(directory.ParentDirectoryId, directory.Name, directory.Id, allDirectoriesQueryable))
            {
                throw new DirectoryListingException("Directory already exists");
            }

            this._context.Update(directory);
            await this._context.SaveChangesAsync();
        }

        /// <summary>
        /// DO NOT update the Name or Filename on TDocument directly, this method will move the physical file if needed. Use RenameDocument if you want to do this yourself.
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="document"></param>
        /// <param name="newName"></param>
        /// <param name="allDocumentsQueryable"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryListingException"></exception>
        public async Task UpdateDocumentAsync<TDocument>(TDocument document, string newName, IQueryable<TDocument>? allDocumentsQueryable = null) where TDocument : Data.Document
        {
            if (await this.DocumentExistsAsync(document.DirectoryId, newName, document.Id, allDocumentsQueryable))
            {
                throw new DirectoryListingException("Document already exists");
            }

            this.RenameDocument(document, newName);

            this._context.Update(document);
            await this._context.SaveChangesAsync();
        }
    }
}
