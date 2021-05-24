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
        private readonly Data.IImagesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FileService(IDbContextFetcher dbContextFetcher, IWebHostEnvironment environment)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IImagesDbContext>();
            this._environment = environment;
        }

        public async Task SaveFormFilesAsync<TFile>(ViewModels.EditorTemplates.MultiFile viewModel, IEnumerable<TFile> existingFiles, Action<TFile>? setParams = null) where TFile : Data.Models.File, new()
        {
            var fileUIDsToKeep = viewModel.Files.ToList();

            var uploadSort = 1000;
            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var uploadedFile = await this.UploadFileAsync(typeof(TFile), file, x =>
                {
                    x.Sort = uploadSort++;
                    setParams?.Invoke((TFile)x);
                });

                fileUIDsToKeep.Add(uploadedFile.UID);
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
        }

        public async Task<T> UploadFileAsync<T>(IFormFile file, Action<T>? setParams = null) where T : Data.Models.File, new()
        {
            return (T)await UploadFileAsync(typeof(T), file, x => setParams?.Invoke((T)x));
        }

        public async Task<Data.Models.File> UploadFileAsync(Type t, IFormFile file, Action<Data.Models.File>? setParams = null)
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);

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

            return dbFile;
        }
    }
}
