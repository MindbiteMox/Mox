using Microsoft.AspNetCore.Http;
using Mindbite.Mox.DesignDemoApp.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DesignDemoApp.ViewModels
{
    public class ImagesViewModel
    {
        public Design Design { get; set; }
        public ImageFormViewModel ImageForm { get; set; }
    }

    public class ImageFormViewModel
    {
        [Required]
        [Display(Name = "Bildtitel")]
        public string Title { get; set; }
        [Required]
        public IFormFile Image { get; set; }
    }
}
