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
    public class NotificationsController : Controller
    {
        private UserManager<MoxUser> _userManager;
        private INotificationReciever _notificationReciever;

        public NotificationsController(UserManager<MoxUser> userManager, INotificationReciever notificationReciever)
        {
            this._userManager = userManager;
            this._notificationReciever = notificationReciever;
        }

        public async Task<IActionResult> Index(int? page, string sortColumn, string sortDirection, string filter)
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dataSource = this._notificationReciever.FetchHistory(me);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                dataSource = dataSource.Where(x => x.SubjectId.ToLower() == filter.ToLower());
            }

            var dataTable = DataTableBuilder
                .Create(dataSource)
                .Sort(sortColumn ?? "SubjectId", sortDirection ?? "Ascending")
                .Page(page)
                .RowLink(x => x.URL)
                .Columns(columns =>
                {
                    columns.Add(x => x.Id).Title("Id").Width(100);
                    columns.Add(x => x.SubjectId).Title("Ämne").Width(200);
                    columns.Add(x => x.ShortDescription).Title("Beskrivning");
                });

            return this.ViewOrOk(dataTable);
        }

        public async Task<IActionResult> GetUnread()
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dataSource = this._notificationReciever.FetchUnread(me);

            return Ok(dataSource.Select(x => new { title = x.ShortDescription, url = x.URL }));
        }

        public async Task<IActionResult> MarkAsUnread()
        {
            var me = await this._userManager.FindByIdAsync(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await this._notificationReciever.MarkAllReadAsync(me);
            return Ok("OK");
        }
    }
}
