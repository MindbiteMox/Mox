using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Identity;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DesignDemoApp.IdentityExtensions
{
    public class UserImage : SettingsOptions.SettingsExtension<ViewModels.UserImage>
    {
        private readonly Data.IDesignDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserImage(IDbContextFetcher dbContextFetcher, IWebHostEnvironment webHostEnvironment)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IDesignDbContext>();
            this._webHostEnvironment = webHostEnvironment;
        }

        public override async Task<object> GetViewModel(string userId)
        {
            var userImage = await this._context.UserImages.FirstOrDefaultAsync(x => x.UserId == userId);

            if(userImage == null)
                return new ViewModels.UserImage();

            return new ViewModels.UserImage
            {
                ImageUrl = userImage.FileUrl
            };
        }

        public override async Task Save(string userId, object viewModel, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            var userImageViewModel = (ViewModels.UserImage)viewModel;

            var userImage = await this._context.UserImages.FirstOrDefaultAsync(x => x.UserId == userId);
            if (userImage != null)
            {
                this._context.Remove(userImage);
                await this._context.SaveChangesAsync();
            }

            var newUserImage = new Data.Models.UserImage
            {
                UserId = userId,
                Filename = userImageViewModel.Upload.FileName
            };

            var filePath = System.IO.Path.Combine(this._webHostEnvironment.WebRootPath, newUserImage.FilePath);
            var fileDir = System.IO.Path.GetDirectoryName(filePath);
            System.IO.Directory.CreateDirectory(fileDir);

            using (var fileStream = System.IO.File.OpenWrite(filePath))
            using (var uploadStream = userImageViewModel.Upload.OpenReadStream())
            {
                await uploadStream.CopyToAsync(fileStream);
            }

            this._context.Add(newUserImage);
            await this._context.SaveChangesAsync();
        }
    }
}
