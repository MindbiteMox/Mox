using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Images.Attributes;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Services
{
    public class FileService
    {
        public class UploadResult<T> where T : Data.Models.File
        {
            public IFormFile FormFile { get; set; }
            public T? File { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private readonly Data.IImagesDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly MoxImageOptions _options;

        public FileService(IDbContextFetcher dbContextFetcher, IWebHostEnvironment environment, Microsoft.Extensions.Options.IOptions<MoxImageOptions> options)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IImagesDbContext>();
            this._environment = environment;
            this._options = options.Value;
        }

        public async Task<IEnumerable<UploadResult<TFile>>> SaveFormFilesAsync<TFile>(ViewModels.EditorTemplates.MultiFile viewModel, IEnumerable<TFile> existingFiles, Action<TFile>? setParams = null) where TFile : Data.Models.File, new()
        {
            var fileUIDsToKeep = viewModel.Files.ToList();
            var results = new List<UploadResult<TFile>>();

            var uploadSort = 1000;
            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var uploadedFile = await this.UploadFileAsync<TFile>(file, x =>
                {
                    x.Sort = uploadSort++;
                    setParams?.Invoke(x);
                });

                results.Add(uploadedFile);
                if(uploadedFile.File != null)
                {
                    fileUIDsToKeep.Add(uploadedFile.File.UID);
                }
            }

            var existingFileUIDs = existingFiles.Select(x => x.UID);
            var allFilesUIDs = fileUIDsToKeep.Concat(existingFileUIDs).Distinct().ToList();
            var storedFiles = await this._context.AllFiles.Where(x => allFilesUIDs.Contains(x.UID)).ToListAsync();

            foreach (var storedFile in storedFiles)
            {
                if (fileUIDsToKeep.Contains(storedFile.UID) && storedFile is TFile storedTFile)
                {
                    var sort = fileUIDsToKeep.IndexOf(storedTFile.UID);

                    storedTFile.Sort = sort;
                    setParams?.Invoke(storedTFile);
                    this._context.Update(storedTFile);
                }
                else
                {
                    this._context.Remove(storedFile);
                }
            }

            await this._context.SaveChangesAsync();

            return results;
        }

        public async Task<UploadResult<T>> UploadFileAsync<T>(IFormFile file, Action<T>? setParams = null) where T : Data.Models.File, new()
        {
            var result = await UploadFileAsync(typeof(T), file, x => setParams?.Invoke((T)x));
            return new UploadResult<T>
            {
                File = (T?)result.File,
                ErrorMessage = result.ErrorMessage,
                FormFile = file
            };
        }

        public async Task<UploadResult<Data.Models.File>> UploadFileAsync(Type t, IFormFile file, Action<Data.Models.File>? setParams = null)
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);

            var allow = this._options.AllowUpload?.Invoke(t, file, ms);
            if (!string.IsNullOrWhiteSpace(allow))
            {
                return new UploadResult<Data.Models.File>
                {
                    ErrorMessage = allow
                };
            }

            var dbFile = (Data.Models.File)Activator.CreateInstance(t)!;
            dbFile.FileName = file.FileName;
            dbFile.ContentType = file.ContentType;
            setParams?.Invoke(dbFile);
            this._context.Add(dbFile);
            await this._context.SaveChangesAsync();

            var originalFilePath = dbFile.FilePath(this._environment);
            Directory.CreateDirectory(Path.GetDirectoryName(originalFilePath));

            {
                using var fs = new FileStream(originalFilePath, FileMode.Create);
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(fs);
            }

            return new UploadResult<Data.Models.File>
            {
                File = dbFile,
                FormFile = file
            };
        }
    }
}
