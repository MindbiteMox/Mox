using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Reporting.Services;
using Newtonsoft.Json;


namespace Mindbite.Mox.Reporting.Areas.Reports.Controllers
{
    [Area(Configuration.MainArea)]
    public class ReportController : Controller
    {
        private readonly ReportingService _reportingService;

        public ReportController(ReportingService reportingService)
        {
            this._reportingService = reportingService;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
