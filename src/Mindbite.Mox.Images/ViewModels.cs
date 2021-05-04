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
        public string? Url { get; set; }
        public bool Delete { get; set; }
        [Display(Name = "Ladda upp bild")]
        public IFormFile? File { get; set; }
    }
}
