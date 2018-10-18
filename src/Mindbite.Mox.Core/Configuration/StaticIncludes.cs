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

    public class StaticFile
    {
        public string WebRootRelativePath { get; set; }

        private FileVersionHash HashType { get; set; }
        private string FileHash { get; set; }

        private string TagName { get; set; }
        private bool OpenTag { get; set; }
        private string UrlAttributeName { get; set; }
        private Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        private StaticFile()
        {

        }

        private void UpdateHash(FileVersionHash withHash, IEnumerable<IFileProvider> fileProviders)
        {
            if(this.HashType != withHash)
            {
                this.HashType = withHash;
                switch(this.HashType)
                {
                    case FileVersionHash.FileHash:
                        var file = fileProviders.Select(x => x.GetFileInfo(this.WebRootRelativePath)).FirstOrDefault(x => x.Exists);
                        if(file == null)
                        {
                            throw new Exception($"Static file {this.WebRootRelativePath} could not be found!");
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

            if(this.OpenTag)
            {
                return new HtmlString($"<{TagName} {UrlAttributeName}=\"{(root != "/" ? root : "")}/{this.WebRootRelativePath.TrimStart('~', '/')}{hash}\" {string.Join(" ", Attributes.Select(x => $"{x.Key}=\"{x.Value}\""))}></{TagName}>");
            }
            else
            {
                return new HtmlString($"<{TagName} {UrlAttributeName}=\"{(root != "/" ? root : "")}/{this.WebRootRelativePath.TrimStart('~', '/')}{hash}\" {string.Join(" ", Attributes.Select(x => $"{x.Key}=\"{x.Value}\""))} />");
            }
        }

        public static StaticFile Style(string webRootRelativePath, int minWidth = int.MinValue, int maxWidth = int.MaxValue)
        {
            string getMediaQuery()
            {
                if (minWidth == int.MinValue && maxWidth == int.MaxValue)
                    return "";

                var queries = new List<string>();

                if (minWidth > int.MinValue)
                    queries.Add($"(min-width: {minWidth}px)");

                if (maxWidth < int.MaxValue)
                    queries.Add($"(max-width: {maxWidth}px)");

                return $"screen and {string.Join(" and ", queries)}";
            }

            return new StaticFile
            {
                WebRootRelativePath = webRootRelativePath,
                OpenTag = false,
                TagName = "link",
                UrlAttributeName = "href",
                Attributes = new Dictionary<string, string>
                {
                    { "rel", "stylesheet" },
                    { "media", getMediaQuery() }
                }
            };
        }

        public static StaticFile Script(string webRootRelativePath)
        {
            return new StaticFile
            {
                WebRootRelativePath = webRootRelativePath,
                OpenTag = true,
                TagName = "script",
                UrlAttributeName = "src",
            };
        }
    }

    public class StaticFileProviderOptions
    {
        public List<IFileProvider> FileProviders { get; private set; } = new List<IFileProvider>();
    }

    public class IncludeConfig
    {
        public List<StaticFile> Files { get; private set; }
        public string StaticRoot { get; set; }

        public IncludeConfig()
        {
            this.Files = new List<StaticFile>();
            this.StaticRoot = "/";
        }
    }
}
