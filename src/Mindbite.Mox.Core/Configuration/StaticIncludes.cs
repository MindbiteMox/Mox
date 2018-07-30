using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.Configuration.StaticIncludes
{
    public enum FileVersionHash
    {
        None,
        FileHash,
        Random
    }

    public class Style
    {
        public string webRootRelativePath { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }

        private FileVersionHash HashType { get; set; }
        private string FileHash { get; set; }

        public string MediaQuery
        {
            get
            {
                if (this.MinWidth == int.MinValue && this.MaxWidth == int.MaxValue)
                    return "";

                var queries = new List<string>();

                if (this.MinWidth > int.MinValue)
                    queries.Add($"(min-width: {this.MinWidth}px)");

                if(this.MaxWidth < int.MaxValue)
                    queries.Add($"(max-width: {this.MaxWidth}px)");

                return $"screen and {string.Join(" and ", queries)}";
            }
        }

        public Style(string webRootRelativePath, int minWidth = int.MinValue, int maxWidth = int.MaxValue)
        {
            this.webRootRelativePath = webRootRelativePath;
            this.MinWidth = minWidth;
            this.MaxWidth = maxWidth;
            this.HashType = FileVersionHash.None;
        }

        private void UpdateHash(FileVersionHash withHash, IEnumerable<IFileProvider> fileProviders)
        {
            if(this.HashType != withHash)
            {
                this.HashType = withHash;
                switch(this.HashType)
                {
                    case FileVersionHash.FileHash:
                        var file = fileProviders.Select(x => x.GetFileInfo(this.webRootRelativePath)).FirstOrDefault(x => x.Exists);
                        if(file == null)
                        {
                            throw new Exception($"Script file {this.webRootRelativePath} could not be found!");
                        }

                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        using (var fileStream = file.CreateReadStream())
                        {
                            var hash = md5.ComputeHash(fileStream);
                            this.FileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                        break;
                    case FileVersionHash.Random:
                        this.FileHash = new Random().Next().ToString();
                        break;
                    case FileVersionHash.None:
                        this.FileHash = "";
                        break;
                }
            }
        }

        public HtmlString Render(string staticRoot, IEnumerable<IFileProvider> fileProviders, FileVersionHash withHash = FileVersionHash.None)
        {
            string hash = "";

            this.UpdateHash(withHash, fileProviders);

            if (withHash != FileVersionHash.None)
            {
                hash = $"?hash={this.FileHash}";
            }

            string root = $"/{staticRoot.Trim('/')}";

            return new HtmlString($"<link href=\"{(root != "/" ? root : "")}/{this.webRootRelativePath.TrimStart('~', '/')}{hash}\" rel=\"stylesheet\" media=\"{this.MediaQuery}\" />");
        }
    }

    public class StaticFileProviderOptions
    {
        public List<IFileProvider> FileProviders { get; private set; } = new List<IFileProvider>();
    }

    public class IncludeConfig
    {
        public List<string> Scripts { get; private set; }
        public List<Style> Styles { get; private set; }
        public string StaticRoot { get; set; }

        public IncludeConfig()
        {
            this.Scripts = new List<string>();
            this.Styles = new List<Style>();
            this.StaticRoot = "/";
        }
    }
}
