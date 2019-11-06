using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Mindbite.Mox.Utils.FileProviders
{
    public class EmbeddedFilesInAssemblyFileProvider : IFileProvider
    {
        private readonly EmbeddedFileProvider _embeddedFileProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EmbeddedFilesInAssemblyFileProvider(Assembly assembly, IWebHostEnvironment webHostEnvironment)
        {
            this._embeddedFileProvider = new EmbeddedFileProvider(assembly);
            this._webHostEnvironment = webHostEnvironment;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this._embeddedFileProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Look for file in the current hosting environment (Your app project), if a file is found, don't look for it in the assembly
            if (this._webHostEnvironment != null)
            {
                var filepath = Path.Combine(this._webHostEnvironment.ContentRootPath, subpath.TrimStart('/'));
                if (File.Exists(filepath))
                    return new NotFoundFileInfo(filepath);
            }

            return this._embeddedFileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return this._embeddedFileProvider.Watch(filter);
        }
    }

    public class StaticFilesInAssemblyFileProvider : IFileProvider
    {
        private readonly EmbeddedFileProvider _embeddedFileProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _staticRoot;

        public StaticFilesInAssemblyFileProvider(Assembly assembly, IWebHostEnvironment webHostEnvironment, string staticRoot = "/wwwroot")
        {
            this._embeddedFileProvider = new EmbeddedFileProvider(assembly);
            this._webHostEnvironment = webHostEnvironment;
            this._staticRoot = staticRoot;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this._embeddedFileProvider.GetDirectoryContents(Path.Combine(this._staticRoot, subpath.TrimStart('/')));
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Look for file in the current hosting environment (Your app project), if a file is found, don't look for it in the assembly
            if (this._webHostEnvironment != null)
            {
                var filepath = Path.Combine(this._webHostEnvironment.WebRootPath, subpath.TrimStart('/'));
                if (File.Exists(filepath))
                    return new NotFoundFileInfo(filepath);
            }

            return this._embeddedFileProvider.GetFileInfo(Path.Combine(this._staticRoot, subpath.TrimStart('/')));
        }

        public IChangeToken Watch(string filter)
        {
            return this._embeddedFileProvider.Watch(filter);
        }
    }
}
