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
    { }

    public interface IDirectoryListingDbContext<TDocument, TDirectory> : IDirectoryListingDbContext where TDocument : Document<TDirectory> where TDirectory : DocumentDirectory<TDocument, TDirectory> 
    {
        DbSet<TDocument> AllDocuments { get; }
        DbSet<TDirectory> AllDocumentDirectories { get; }
    }

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

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }

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
                    ".zip" => "fa-archive",
                    _ => "fa-file-alt"
                };
                return $"<i class='far {fileIcon}'></i>";
            }
        }
    }

    public abstract class Document<TDirectory> : Document where TDirectory : DocumentDirectory
    {
        public int? DirectoryId { get; set; }

        public TDirectory? Directory { get; set; }
    }

    public abstract class DocumentDirectory : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid UID { get; set; }

        public string Name { get; set; } = string.Empty;
        public int? LegacyId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedById { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedById { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedById { get; set; }
    }

    public abstract class DocumentDirectory<TDirectory> : DocumentDirectory where TDirectory : DocumentDirectory<TDirectory>
    {
        public int? ParentDirectoryId { get; set; }
        public TDirectory? ParentDirectory { get; set; }
        public ICollection<TDirectory> ChildDirectories { get; set; } = new List<TDirectory>();
    }

    public abstract class DocumentDirectory<TDocument, TDirectory> : DocumentDirectory<TDirectory> where TDocument : Document<TDirectory> where TDirectory : DocumentDirectory<TDocument, TDirectory>
    {
        public ICollection<TDocument> Documents { get; set; } = new List<TDocument>();
    }
}
