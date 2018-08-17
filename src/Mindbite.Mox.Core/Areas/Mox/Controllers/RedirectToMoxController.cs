using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mindbite.Mox.Controllers
{
    [Area("Mox")]
    public class RedirectToMoxController : Controller
    {
        private readonly Microsoft.Extensions.Options.IOptions<Configuration.Config> _config;

        public RedirectToMoxController(Microsoft.Extensions.Options.IOptions<Configuration.Config> config)
        {
            this._config = config;
        }

        public IActionResult Index()
        {
            if (this.Request.Path.StartsWithSegments(new PathString($"/{this._config.Value.Path}")))
                return NotFound();
            return LocalRedirect($"/{this._config.Value.Path}");
        }
    }
}
