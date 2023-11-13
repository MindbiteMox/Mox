using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Mindbite.Mox.Anonymization.Controllers
{
    [AllowAnonymous]
    public class DeferredAnonymizationController : Controller
    {
        private readonly Services.IDeferredAnonymizationService _anonymizationService;

        public DeferredAnonymizationController(Services.IDeferredAnonymizationService anonymizationService)
        {
            this._anonymizationService = anonymizationService;
        }

        public async Task<IActionResult> Anonymize()
        {
            await this._anonymizationService.AnonymizeDeferredAsync();

            return Ok();
        }
    }
}
