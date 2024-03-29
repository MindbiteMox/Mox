﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Extensions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using Mindbite.Mox.DirectoryListing.ViewModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mindbite.Mox.DirectoryListing.Controllers
{
    public abstract class DirectoryListingControllerBase<TDocument, TDirectory> : Controller where TDocument : Data.Document<TDirectory> where TDirectory : Data.DocumentDirectory<TDocument, TDirectory>
    {
        private readonly Data.IDirectoryListingDbContext<TDocument, TDirectory> _context;

        protected virtual IQueryable<TDirectory> GetDirectories(Data.IDirectoryListingDbContext<TDocument, TDirectory>? dbContext = null) => (dbContext ?? this._context).Set<TDirectory>();
        protected virtual IQueryable<TDocument> GetDocuments(Data.IDirectoryListingDbContext<TDocument, TDirectory>? dbContext = null) => (dbContext ?? this._context).Set<TDocument>();
        protected virtual Task DocumentBeforeAddAsync(TDocument document) => Task.CompletedTask;
        protected virtual Task DocumentAfterAddAsync(TDocument document) => Task.CompletedTask;
        protected virtual Task DocumentBeforeUpdateAsync(TDocument document) => Task.CompletedTask;
        protected virtual Task DocumentAfterUpdateAsync(TDocument document) => Task.CompletedTask;
        protected virtual Task DocumentBeforeDownloadAsync(TDocument document) => Task.CompletedTask;
        protected virtual Task DirectoryBeforeAddAsync(TDirectory document) => Task.CompletedTask;
        protected virtual Task DirectoryAfterAddAsync(TDirectory document) => Task.CompletedTask;
        protected virtual Task DirectoryBeforeUpdateAsync(TDirectory document) => Task.CompletedTask;
        protected virtual Task DirectoryAfterUpdateAsync(TDirectory document) => Task.CompletedTask;
        protected virtual Task DirectoryBeforeDownloadAsync(TDirectory? rootDirectory, IEnumerable<TDocument> allDownloadedDocuments) => Task.CompletedTask;

        public DirectoryListingControllerBase(Mindbite.Mox.Services.IDbContextFetcher context)
        {
            this._context = context.FetchDbContext<Data.IDirectoryListingDbContext<TDocument, TDirectory>>();
        }
    }
}
