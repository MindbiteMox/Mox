using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mindbite.Mox.Images.Attributes;
using Mindbite.Mox.Images.Data.Models;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Data
{
    public interface IImagesDbContext : Core.Data.IDbContext
    {
        DbSet<Image> AllImages { get; }
        DbSet<File> AllFiles { get; }
    }

    public class ImageEntityTypeConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.Property(x => x.UID).HasDefaultValueSql("NEWID()");
        }
    }

    public class FileEntityTypeConfiguration : IEntityTypeConfiguration<File>
    {
        public void Configure(EntityTypeBuilder<File> builder)
        {
            builder.Property(x => x.UID).HasDefaultValueSql("NEWID()");
        }
    }
}

namespace Mindbite.Mox.Images.Data.Models
{
    [Table(nameof(Image), Schema = "Mox")]
    public abstract class Image : Core.Data.Models.ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid UID { get; set; }

        public string ContentType { get; set; }
        public string FileName { get; set; }
        public int Sort { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        [NotMapped]
        public abstract string FileDirectory { get; }

        public string GetFileNameForSize(string? sizeName)
        {
            if (!string.IsNullOrWhiteSpace(sizeName))
            {
                var size = this.GetType().GetCustomAttributes(typeof(ImageSizeAttribute), false).Cast<ImageSizeAttribute>().FirstOrDefault(x => x.Name.Equals(sizeName, StringComparison.OrdinalIgnoreCase));

                if (size != null)
                {
                    var extension = size.AlphaMode == ImageAlphaMode.Retain ? (Services.ImageService.HasAlpha(this.FileName) ? ".png" : ".jpg") : ".jpg";
                    return $"{this.UID}_{size.Name}{extension}";
                }
            }

            return $"{this.UID}{System.IO.Path.GetExtension(this.FileName)}";
        }

        public abstract string DefaultSizeFileUrl { get; }
        public string FileUrl(string? sizeName) => $"/Media/{this.FileDirectory.Trim('/')}/{this.GetFileNameForSize(sizeName)}";
        public string FilePath(IWebHostEnvironment env, string? sizeName) => System.IO.Path.Combine(env.WebRootPath, "Media", this.FileDirectory, this.GetFileNameForSize(sizeName));
    }

    [Table(nameof(File), Schema = "Mox")]
    public abstract class File : Core.Data.Models.ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid UID { get; set; }

        public string ContentType { get; set; }
        public string FileName { get; set; }
        public int Sort { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        [NotMapped]
        public virtual string FileDirectory => this.GetType().Name;
        [NotMapped]
        public string FileUrl => $"/Media/{this.FileDirectory.Trim('/')}/{this.UID}/{this.FileName}";
        public string FilePath(IWebHostEnvironment env) => System.IO.Path.Combine(env.WebRootPath, "Media", this.FileDirectory, this.UID.ToString(), this.FileName);
    }
}

namespace Mindbite.Mox.Images.Internal.Data
{
    public class DummyImageEntityTypeConfiguration : IEntityTypeConfiguration<_DummyImage>
    {
        public void Configure(EntityTypeBuilder<_DummyImage> builder) { }
    }

    public class DummyFileEntityTypeConfiguration : IEntityTypeConfiguration<_DummyFile>
    {
        public void Configure(EntityTypeBuilder<_DummyFile> builder) { }
    }

    public class _DummyFile : File { }
    public class _DummyImage : Image
    {
        public override string FileDirectory => "_DummyImage";
        public override string DefaultSizeFileUrl => this.FileUrl(null);
    }
}