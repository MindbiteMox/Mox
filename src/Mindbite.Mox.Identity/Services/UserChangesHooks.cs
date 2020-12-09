using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public class UserChanges
    {
        public virtual Task OnCreateAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnUpdateAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnDeleteAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnCreatedAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnUpdatedAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnDeletedAsync(MoxUser user) { return Task.CompletedTask; }
        public virtual Task OnLoginRefreshedAsync(MoxUser user) { return Task.CompletedTask; }
    }
}
