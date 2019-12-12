using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Core.Models
{
    public interface ISoftDeleted
    {
        bool IsDeleted { get; set; }
        DateTime CreatedOn { get; set; }
        string? CreatedById { get; set; }
        DateTime ModifiedOn { get; set; }
        string? ModifiedById { get; set; }
        DateTime? DeletedOn { get; set; }
        string? DeletedById { get; set; }

    }

    public interface IUIDEntity
    {
        int Id { get; set; }
        Guid UID { get; set; }
    }
}
