using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.ViewModels.EditorTemplates
{
    public class SingleImage
    {
        public string ImageTypeFullName { get; set; }
        public string? Url { get; set; }
        public bool Delete { get; set; }
        [Display(Name = "Ladda upp bild")]
        public IFormFile? File { get; set; }

        public static SingleImage ForType<TFile>() where TFile : Data.Models.Image
        {
            return new SingleImage
            {
                ImageTypeFullName = typeof(TFile).FullName!,
            };
        }

        public static SingleImage From<TFile>(TFile? image) where TFile : Data.Models.Image
        { 
            return new SingleImage
            {
                ImageTypeFullName = typeof(TFile).FullName!,
                Url = image?.DefaultSizeFileUrl
            };
        }
    }

    public class MultiImage
    {
        public string ImageTypeFullName { get; set; }
        public IFormFile[]? Upload { get; set; }
        public Guid[] Images { get; set; } = Array.Empty<Guid>();

        public static MultiImage ForType<TFile>() where TFile : Data.Models.Image
        {
            return new MultiImage
            {
                ImageTypeFullName = typeof(TFile).FullName!,
            };
        }

        public static MultiImage From<TFile>(IEnumerable<TFile> image) where TFile : Data.Models.Image
        {
            return new MultiImage
            {
                ImageTypeFullName = typeof(TFile).FullName!,
                Images = image.OrderBy(x => x.Sort).Select(x => x.UID).ToArray()
            };
        }
    }

    public class MultiFile
    {
        public string FileTypeFullName { get; set; }
        public IFormFile[]? Upload { get; set; }
        public Guid[] Files { get; set; } = Array.Empty<Guid>();

        public static MultiFile ForType<TFile>() where TFile : Data.Models.File
        {
            return new MultiFile
            {
                FileTypeFullName = typeof(TFile).FullName!,
            };
        }

        public static MultiFile From<TFile>(IEnumerable<TFile> files) where TFile : Data.Models.File
        {
            return new MultiFile
            {
                FileTypeFullName = typeof(TFile).FullName!,
                Files = files.OrderBy(x => x.Sort).Select(x => x.UID).ToArray()
            };
        }
    }
}
