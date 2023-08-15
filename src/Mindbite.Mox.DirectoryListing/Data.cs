using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mindbite.Mox.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Mindbite.Mox.DirectoryListing.Data
{
    public interface IDirectoryListingDbContext : Core.Data.IDbContext
    {
        DbSet<Document> AllDocuments { get; set; }
        DbSet<DocumentDirectory> AllDocumentDirectories { get; set; }
    }

    public class DocumentEntityTypeConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.Property(x => x.UID).HasDefaultValueSql("NEWID()");
        }
    }

    public class DocumentDirectoryEntityTypeConfiguration : IEntityTypeConfiguration<DocumentDirectory>
    {
        public void Configure(EntityTypeBuilder<DocumentDirectory> builder)
        {
            builder.Property(x => x.UID).HasDefaultValueSql("NEWID()");
        }
    }

    [Table(nameof(Document), Schema = "App")]
    public abstract class Document : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid UID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DirectoryId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        public DocumentDirectory? Directory { get; set; }

        public virtual string VirtualFilePath(DocumentServiceOptions option) => $"~/{option.GetStoreDirectory(this).Trim('/')}/{this.UID}/{this.FileName}";
        public virtual string FilePath(DocumentServiceOptions option, IWebHostEnvironment env) => $"{env.WebRootPath}/{option.GetStoreDirectory(this).Trim('/')}/{this.UID}/{this.FileName}";
        public virtual string FileUrl(DocumentServiceOptions option) => $"/{option.GetStoreDirectory(this).Trim('/')}/{this.UID}/{this.FileName}";

        [NotMapped]
        public virtual string Extension => Path.GetExtension(this.FileName) ?? string.Empty;

        [NotMapped]
        public virtual string Icon
        {
            get
            {
                var fileIcon = this.Extension switch
                {
                    ".xls" => "fa-file-excel",
                    ".xlsx" => "fa-file-excel",
                    ".doc" => "fa-file-word",
                    ".docx" => "fa-file-word",
                    ".pdf" => "fa-file-pdf",
                    ".jpg" => "fa-file-image",
                    ".jpeg" => "fa-file-image",
                    ".png" => "fa-file-image",
                    _ => "fa-file-alt"
                };
                return $"<i class='far {fileIcon}'></i>";
            }
        }

    }

    [Table(nameof(DocumentDirectory), Schema = "App")]
    public abstract class DocumentDirectory : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid UID { get; set; }

        public string Name { get; set; } = string.Empty;
        public int? ParentDirectoryId { get; set; }
        public int? LegacyId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<DocumentDirectory> ChildDirectories { get; set; } = new List<DocumentDirectory>();
        public DocumentDirectory? ParentDirectory { get; set; }
    }
}
