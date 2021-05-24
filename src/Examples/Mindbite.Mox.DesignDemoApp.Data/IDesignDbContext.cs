using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.DesignDemoApp.Data.Models;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mindbite.Mox.DesignDemoApp.Data
{
    public interface IDesignDbContext : Core.Data.IDbContext
    {
        DbSet<Design> Designs { get; }
        DbSet<Image> Images { get; }
        DbSet<UserImage> UserImages { get; }
    }
}
