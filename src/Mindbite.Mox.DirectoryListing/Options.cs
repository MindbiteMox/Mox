using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Mindbite.Mox.DirectoryListing
{
    public class DocumentServiceOptions
    {
        public string StoreDirectory { get; set; } = "Media/Document";
        public Func<Data.Document, string> GetStoreDirectory { get; set; }
        public Func<HttpContext, Data.Document, Task<bool>> AuthorizeDownloadAsync { get; set; } = (_, _) => Task.FromResult(true);

        public DocumentServiceOptions()
        {
             this.GetStoreDirectory = _ => StoreDirectory;
        }
    }
}
