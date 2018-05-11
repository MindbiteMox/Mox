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
        private EmbeddedFileProvider EmbeddedFileProvider;
        private IHostingEnvironment HostingEnvironment;

        public EmbeddedFilesInAssemblyFileProvider(Assembly assembly, IHostingEnvironment hostingEnvironment)
        {
            this.EmbeddedFileProvider = new EmbeddedFileProvider(assembly);
            this.HostingEnvironment = hostingEnvironment;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this.EmbeddedFileProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Look for file in the current hosting environment (Your app project), if a file is found, don't look for it in the assembly
            if (this.HostingEnvironment != null)
            {
                var filepath = Path.Combine(this.HostingEnvironment.ContentRootPath, subpath.TrimStart('/'));
                if (File.Exists(filepath))
                    return new NotFoundFileInfo(filepath);
            }

            return this.EmbeddedFileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return this.EmbeddedFileProvider.Watch(filter);
        }
    }

    public class StaticFilesInAssemblyFileProvider : IFileProvider
    {
        private EmbeddedFileProvider EmbeddedFileProvider;
        private IHostingEnvironment HostingEnvironment;
        private string StaticRoot;

        public StaticFilesInAssemblyFileProvider(Assembly assembly, IHostingEnvironment hostingEnvironment, string staticRoot = "/wwwroot")
        {
            this.EmbeddedFileProvider = new EmbeddedFileProvider(assembly);
            this.HostingEnvironment = hostingEnvironment;
            this.StaticRoot = staticRoot;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this.EmbeddedFileProvider.GetDirectoryContents(Path.Combine(this.StaticRoot, subpath.TrimStart('/')));
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // Look for file in the current hosting environment (Your app project), if a file is found, don't look for it in the assembly
            if (this.HostingEnvironment != null)
            {
                var filepath = Path.Combine(this.HostingEnvironment.WebRootPath, subpath.TrimStart('/'));
                if (File.Exists(filepath))
                    return new NotFoundFileInfo(filepath);
            }

            return this.EmbeddedFileProvider.GetFileInfo(Path.Combine(this.StaticRoot, subpath.TrimStart('/')));
        }

        public IChangeToken Watch(string filter)
        {
            return this.EmbeddedFileProvider.Watch(filter);
        }
    }
}
