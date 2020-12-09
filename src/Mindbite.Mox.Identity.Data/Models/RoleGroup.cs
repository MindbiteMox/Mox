using Mindbite.Mox.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Mindbite.Mox.Identity.Data.Models
{
    public class RoleGroup : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string GroupName { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        public ICollection<RoleGroupRole> Roles { get; set; } = new List<RoleGroupRole>();
        public ICollection<MoxUser> Users { get; set; } = new List<MoxUser>();
    }

    public class RoleGroupRole : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RoleGroupId { get; set; }
        public string Role { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        public RoleGroup? RoleGroup { get; set; }
    }
}
