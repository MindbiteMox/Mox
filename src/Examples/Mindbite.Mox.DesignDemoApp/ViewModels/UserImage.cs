using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DesignDemoApp.ViewModels
{
    public class UserImage
    {
        public string ImageUrl { get; set; }
        public IFormFile Upload { get; set; }
    }
}
