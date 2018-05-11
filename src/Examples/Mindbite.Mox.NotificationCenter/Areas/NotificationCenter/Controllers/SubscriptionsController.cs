using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Notifications;
using Mindbite.Mox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mindbite.Mox.NotificationCenter.Areas.NotificationCenter.Controllers
{
    [Area(Constants.MainArea)]
    public class SubscriptionsController : Controller
    {
        private UserManager<MoxUser> _userManager;
        private INotificationSubscriber _notificationSubscriber;

        public SubscriptionsController(UserManager<MoxUser> userManager, INotificationSubscriber notificationSubscriber)
        {
            this._userManager = userManager;
            this._notificationSubscriber = notificationSubscriber;
        }

        public async Task<IActionResult> Index(int? page, string sortColumn, string sortDirection, string filter)
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dataSource = this._notificationSubscriber.Subscriptions(me);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                dataSource = dataSource.Where(x => x.SubjectId.ToLower() == filter.ToLower());
            }

            var dataTable = DataTableBuilder
                .Create(dataSource)
                .Sort(sortColumn ?? "SubjectId", sortDirection ?? "Ascending")
                .Page(page)
                .Columns(columns =>
                {
                    columns.Add(x => x.Id).Title("Id").Width(100);
                    columns.Add(x => x.SubjectId).Title("Ämne");
                });

            return this.ViewOrOk(dataTable);
        }

        [HttpGet]
        public async Task<IActionResult> SubscribeTo(string subject, int? entityId)
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await this._notificationSubscriber.SubscribeToAsync(me, subject, entityId);

            return Ok("OK");
        }

        [HttpGet]
        public async Task<IActionResult> Subscribing(string subject, int? entityId)
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var isSubscribing = this._notificationSubscriber.Subscriptions(subject, entityId).Any();
            return Ok(isSubscribing);
        }
    }
}
