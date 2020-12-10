using Microsoft.AspNetCore.Mvc.Rendering;
using Mindbite.Mox.Attributes;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Mindbite.Mox.Identity.ViewModels
{
    public class RoleGroupRole
    {
        [Required]
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Checked { get; set; }
        public int Depth { get; set; }
        public bool IsParent { get; set; }
        public bool Disabled { get; set; }
    }

    public class RoleGroupRolesTree
    {
        public RoleGroupRole[] Roles { get; set; } = Array.Empty<RoleGroupRole>();
    }

    public class RoleGroup
    {
        [Display(Name = "Namn")]
        [MoxRequired]
        public string? GroupName { get; set; }
        [Display(Name = "Behörigheter")]
        [MoxFormFieldType(Render.EditorOnly)]
        [MoxFormFieldSet("Behörigheter")]
        public RoleGroupRolesTree RoleTree { get; set; } = new RoleGroupRolesTree();

        public RoleGroup SetRoles(Data.Models.RoleGroup? group, IEnumerable<IdentityExtensions.RoleTreeNode> roles)
        {
            this.RoleTree = new RoleGroupRolesTree
            {
                Roles = roles.Select(x => new RoleGroupRole
                {
                    Id = x.RoleName,
                    Name = x.DisplayName,
                    Checked = x.IsLeaf ? group?.Roles.Any(y => y.Role == x.RoleName) ?? false : roles.Where(y => y.RoleName.StartsWith(x.RoleName) && y.IsLeaf).All(y => group?.Roles.Any(z => z.Role == y.RoleName) ?? false),
                    Depth = x.Depth,
                    IsParent = !x.IsLeaf
                }).ToArray()
            };
            return this;
        }

        public static RoleGroup From(Data.Models.RoleGroup group, IEnumerable<IdentityExtensions.RoleTreeNode> roles)
        {
            return new RoleGroup
            {
                GroupName = group.GroupName,
            }.SetRoles(group, roles);
        }
    }
}
