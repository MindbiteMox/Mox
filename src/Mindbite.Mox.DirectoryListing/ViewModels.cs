﻿using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mindbite.Mox.DirectoryListing.ViewModels
{
    public enum DuplicateAction
    {
        Overwrite,
        KeepBoth,
        Ignore
    }

    public class Document
    {
        [MoxRequired]
        [Display(Name = "Filnamn")]
        public string? Name { get; set; } = string.Empty;

        [Display(Name = "Överliggande mapp")]
        [MoxFormFieldType(Render.DropDown)]
        [MoxFormDataSource("GetDirectories")]
        public int? DirectoryId { get; set; }
    
        public static Document From<TDocument, TDirectory>(TDocument? document) where TDocument : Data.Document<TDirectory> where TDirectory : Data.DocumentDirectory<TDocument, TDirectory>
        {
            return new Document
            {
                Name = document.Name,
                DirectoryId = document.DirectoryId,
            };
        }

        public static async Task<IEnumerable<SelectListItem>> GetDirectories(HttpContext context)
        {
            var dbContext = context.RequestServices.GetRequiredService<Mindbite.Mox.Services.IDbContextFetcher>().FetchDbContext<Data.IDirectoryListingDbContext>();
            var getDirectories = context.Items[Constants.GetDirectoriesHttpContentItemKey] as Func<Data.IDirectoryListingDbContext, IQueryable<Data.DocumentDirectory>>;

            var directories = await getDirectories(dbContext).ToListAsync();

            var directoryType = (Type)context.Items[Constants.DirectoryTypeHttpContentItemKey];
            var tree = Utils.MakeSelectListTree(directoryType, directories, 1);

            return tree.Prepend(new SelectListItem(context.Items[Constants.RootDirectoryNameHttpContentItemKey]!.ToString(), ""));
        }
    }

    public class DocumentDirectory
    {
        [MoxRequired]
        [Display(Name = "Mappens namn")]
        public string? Name { get; set; } = string.Empty;

        [Display(Name = "Överliggande mapp")]
        [MoxFormFieldType(Render.DropDown)]
        [MoxFormDataSource("GetDirectories")]
        public int? ParentDirectoryId { get; set; }

        public static DocumentDirectory From<TDirectory>(TDirectory dir) where TDirectory : Data.DocumentDirectory<TDirectory>
        {
            return new DocumentDirectory
            {
                Name = dir.Name,
                ParentDirectoryId = dir.ParentDirectoryId,
            };
        }

        public static async Task<IEnumerable<SelectListItem>> GetDirectories(HttpContext context)
        {
            var dbContext = context.RequestServices.GetRequiredService<Mindbite.Mox.Services.IDbContextFetcher>().FetchDbContext<Data.IDirectoryListingDbContext>();
            var getDirectories = context.Items[Constants.GetDirectoriesHttpContentItemKey] as Func<Data.IDirectoryListingDbContext, IQueryable<Data.DocumentDirectory>>;

            var directoryId = Guid.TryParse(context.Request.Query["directoryId"], out var _d) ? _d : default(Guid?);
            var directories = await getDirectories(dbContext).ToListAsync();
            var isEditing = context.Request.RouteValues["Action"]!.ToString()!.Equals("EditDirectory", StringComparison.OrdinalIgnoreCase);
            directories = directories.Where(x => !isEditing || directoryId == null || x.UID != directoryId).ToList();

            var directoryType = (Type)context.Items[Constants.DirectoryTypeHttpContentItemKey];
            var tree = Utils.MakeSelectListTree(directoryType, directories, 1);

            return tree.Prepend(new SelectListItem(context.Items[Constants.RootDirectoryNameHttpContentItemKey]!.ToString(), ""));
        }
    }

    public class DocumentUploadDefaultPreflight
    {
    }

    public class DocumentUpload
    {
        public IFormFile[] UploadedFiles { get; set; } = Array.Empty<IFormFile>();
        public DuplicateAction OverwriteFiles { get; set; } = DuplicateAction.Overwrite;
    }

    public class DocumentUpload<TUploadAdditionalData> : DocumentUpload
    {
        public TUploadAdditionalData? AdditionalData { get; set; }
    }

    public class DocumentUploadPreflight<TUploadAdditionalData>
    {
        public string[] FileNames { get; set; } = Array.Empty<string>();
        [MoxRequired]
        public DuplicateAction? OverwriteFiles { get; set; }
        public TUploadAdditionalData? AdditionalData { get; set; }

        public DocumentUploadPreflight()
        {

        }

        public DocumentUploadPreflight(DocumentUpload<TUploadAdditionalData> documentUpload)
        {
            this.FileNames = documentUpload.UploadedFiles.Select(x => x.FileName).ToArray();
            this.OverwriteFiles = documentUpload.OverwriteFiles;
            this.AdditionalData = documentUpload.AdditionalData;
        }
    }

    public class DataTableData
    {
        public Guid UID { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public string Extension { get; set; }
        public string Icon { get; set; }
        public HtmlString PathSort { get; set; }
        public List<(Guid uid, string name)> Path { get; set; } = new List<(Guid uid, string name)>();
        public string NameSort { get; set; }
    }
}
