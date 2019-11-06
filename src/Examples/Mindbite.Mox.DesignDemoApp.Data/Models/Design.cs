using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Hosting;

namespace Mindbite.Mox.DesignDemoApp.Data.Models
{
    [Table(nameof(Design), Schema = "Designs")]
    public class Design
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Titel")]
        public string Title { get; set; }
        [Display(Name = "Beskrivning")]
        public string Description { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Bilder")]
        public ICollection<Image> Images { get; set; }
    }

    public class DesignMapping : IEntityTypeConfiguration<Design>, IEntityTypeConfiguration<Image>, IEntityTypeConfiguration<UserImage>
    {
        public void Configure(EntityTypeBuilder<Design> builder)
        {
            builder.HasMany(x => x.Images)
                .WithOne(x => x.Design)
                .HasForeignKey(x => x.DesignId);
            builder.HasQueryFilter(x => x.IsDeleted == false);
        }

        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasQueryFilter(x => x.IsDeleted == false);
        }

        public void Configure(EntityTypeBuilder<UserImage> builder)
        {
        }
    }

    public class DesignDbContextActions
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DesignDbContextActions(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnvironment = webHostEnvironment;
        }

        public void Remove<TEntity>(IDesignDbContext dbContext, TEntity entity)
        {
            if(entity is UserImage)
            {
                var userImage = entity as UserImage;
                var webroot = this._webHostEnvironment.WebRootPath;
                var filePath = System.IO.Path.Combine(webroot, userImage.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }
}
