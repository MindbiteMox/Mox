using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Images.Attributes;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Services
{
    public class ImageService
    {
        private readonly Data.IImagesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ImageService(IDbContextFetcher dbContextFetcher, IWebHostEnvironment environment)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.IImagesDbContext>();
            this._environment = environment;
        }

        public static bool IsVector(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() switch
            {
                ".svg" => true,
                ".eps" => true,
                _ => false
            };
        }

        public static bool HasAlpha(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() switch
            {
                ".jpg" => false,
                ".jpeg" => false,
                _ => true
            };
        }

        public static bool IsPDF(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() switch
            {
                ".pdf" => true,
                _ => false
            };
        }

        public async Task<T?> SaveFormImageAsync<T>(ViewModels.EditorTemplates.SingleImage viewModel, IEnumerable<T> existingImages, Action<T>? setParams = null) where T : Data.Models.Image, new()
        {
            if (viewModel.File != null)
            {
                try
                {
                    var existingImagesCopy = existingImages.ToList();

                    var theImage = (await this.UploadImageAsync(viewModel.File, (T x) => setParams?.Invoke(x))).First();

                    foreach (var image in existingImagesCopy)
                    {
                        this._context.Remove(image);
                    }
                    await this._context.SaveChangesAsync();

                    return theImage;
                }
                catch { }
            }
            else if (viewModel.Delete)
            {
                foreach (var image in existingImages)
                {
                    this._context.Remove(image);
                }
                await this._context.SaveChangesAsync();
            }

            return null;
        }

        public async Task<TImage?> SaveFormImageAsync<TModel, TImage>(ViewModels.EditorTemplates.SingleImage viewModel, TModel model, Expression<Func<TModel, TImage?>> getExistingImage, Action<TImage>? setParams = null) where TImage : Data.Models.Image, new()
        {
            var compiledGetExistingImage = getExistingImage.Compile();

            if (viewModel.File != null)
            {
                var theImage = (await this.UploadImageAsync(viewModel.File, (TImage x) => setParams?.Invoke(x))).First();

                if (compiledGetExistingImage(model) != null)
                {
                    this._context.Remove(compiledGetExistingImage(model));
                }

                Utils.Dynamics.SetPropertyValue(model, getExistingImage, theImage);

                await this._context.SaveChangesAsync();

                return theImage;
            }
            else if (viewModel.Delete)
            {
                if (compiledGetExistingImage(model) != null)
                {
                    this._context.Remove(compiledGetExistingImage(model));
                }

                Utils.Dynamics.SetPropertyValue(model, getExistingImage, null);

                await this._context.SaveChangesAsync();
            }

            return null;
        }

        public async Task CreateMissingSizesAsync(Type t, Data.Models.Image image)
        {
            var sizes = this.GetSizes(t);

            var missingSizes = sizes.Where(x =>
            {
                var filePath = image.FilePath(this._environment, x.Name);
                return !File.Exists(filePath);
            });

            if (missingSizes.Any())
            {
                var originalPath = image.FilePath(this._environment, null);
                var originalStream = new FileStream(originalPath, FileMode.Open);

                await this.OptimizeImageAsync(t, image, originalStream, sizePredicate: size => missingSizes.Any(y => y.Name == size.Name));
            }
        }

        public async Task CreateMissingSizesAsync<TImage>(TImage image) where TImage : Data.Models.Image
        {
            await CreateMissingSizesAsync(typeof(TImage), image);
        }

        public async Task DeleteDepricatedSizesAsync(Type t, Data.Models.Image image)
        {
            var sizes = this.GetSizes(t);

            var wildcardFileName = Path.GetFileNameWithoutExtension(image.GetFileNameForSize(null)) + "*";
            var directory = Path.GetDirectoryName(image.FilePath(this._environment, null));
            var allSizeFilePaths = Directory.GetFiles(directory, wildcardFileName, SearchOption.TopDirectoryOnly);

            var sizeFilePathsToKeep = sizes.Select(x => image.FilePath(this._environment, x.Name)).Append(image.FilePath(this._environment, null));
            foreach(var file in allSizeFilePaths.Except(sizeFilePathsToKeep))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        public async Task DeleteDepricatedSizesAsync<TImage>(TImage image) where TImage : Data.Models.Image
        {
            await DeleteDepricatedSizesAsync(typeof(TImage), image);
        }

        public async Task SaveFormImagesAsync<TImage>(ViewModels.EditorTemplates.MultiImage viewModel, IEnumerable<TImage> existingImages, Action<TImage>? setParams = null) where TImage : Data.Models.Image, new()
        {
            var imageUIDsToKeep = viewModel.Images.ToList();

            var uploadSort = 1000;
            foreach (var file in viewModel.Upload ?? Array.Empty<IFormFile>())
            {
                var uploadedImages = await this.UploadImageAsync(typeof(TImage), file, x =>
                {
                    x.Sort = uploadSort++;
                    setParams?.Invoke((TImage)x);
                });

                foreach (var uploadedImage in uploadedImages)
                {
                    imageUIDsToKeep.Add(uploadedImage.UID);
                }
            }

            var existingImageUIDs = existingImages.Select(x => x.UID);
            var allImagesUIDs = imageUIDsToKeep.Concat(existingImageUIDs).Distinct().ToList();
            var storedImages = await this._context.AllImages.Where(x => allImagesUIDs.Contains(x.UID)).ToListAsync();

            foreach (var storedImage in storedImages)
            {
                if (imageUIDsToKeep.Contains(storedImage.UID) && storedImage is TImage storedTImage)
                {
                    var sort = imageUIDsToKeep.IndexOf(storedTImage.UID);

                    storedTImage.Sort = sort;
                    setParams?.Invoke(storedTImage);
                    this._context.Update(storedTImage);
                }
                else
                {
                    this._context.Remove(storedImage);
                }
            }

            await this._context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> UploadImageAsync<T>(IFormFile file, Action<T>? setParams = null) where T : Data.Models.Image, new()
        {
            return (await UploadImageAsync(typeof(T), file, x => setParams?.Invoke((T)x))).Cast<T>();
        }

        public async Task<IEnumerable<Data.Models.Image>> UploadImageAsync(Type t, IFormFile file, Action<Data.Models.Image>? setParams = null)
        {
            var pageCount = 1;
            var images = new List<Data.Models.Image>();

            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);

            if(file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var pdfImages = new ImageMagick.MagickImageCollection(ms);
                pageCount = pdfImages.Count;
            }

            foreach(var page in Enumerable.Range(0, pageCount))
            {
                var image = (Data.Models.Image)Activator.CreateInstance(t)!;
                image.FileName = file.FileName;
                image.ContentType = file.ContentType;
                setParams?.Invoke(image);
                this._context.Add(image);
                await this._context.SaveChangesAsync();

                var originalFilePath = image.FilePath(this._environment, null);
                Directory.CreateDirectory(Path.GetDirectoryName(originalFilePath));

                {
                    using var fs = new FileStream(originalFilePath, FileMode.Create);
                    ms.Seek(0, SeekOrigin.Begin);
                    await ms.CopyToAsync(fs);
                }

                {
                    using var fs = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read);
                    await this.OptimizeImageAsync(t, image, fs, page);
                }

                images.Add(image);
            }

            return images;
        }

        public IEnumerable<ImageSizeAttribute> GetSizes(Type imageType)
        {
            if (!imageType.IsAssignableTo(typeof(Data.Models.Image)))
            {
                throw new ArgumentException($"imageType must be of type {typeof(Data.Models.Image).FullName}");
            }

            return imageType.GetCustomAttributes(typeof(ImageSizeAttribute), false).Cast<ImageSizeAttribute>().ToList();
        }

        public IEnumerable<ImageSizeAttribute> GetSizes<TImage>() where TImage : Data.Models.Image
        {
            return GetSizes(typeof(TImage));
        }

        public async Task OptimizeImageAsync<T>(T image, Stream fileStream, int? frameIndex = null, Func<ImageSizeAttribute, bool>? sizePredicate = null) where T : Data.Models.Image
        {
            await OptimizeImageAsync(typeof(T), image, fileStream, frameIndex, sizePredicate);
        }

        public async Task OptimizeImageAsync(Type t, Data.Models.Image image, Stream fileStream, int? frameIndex = null, Func<ImageSizeAttribute, bool>? sizePredicate = null)
        {
            var sizes = this.GetSizes(t);

            var originalFilePath = image.FilePath(this._environment, null);
            var originalFileDirectoryPath = Path.GetDirectoryName(originalFilePath) ?? string.Empty;

            Directory.CreateDirectory(originalFileDirectoryPath);

            var isVector = IsVector(originalFilePath);
            var hasAlpha = HasAlpha(originalFilePath);
            var isPDF = IsPDF(originalFilePath);

            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            foreach (var size in sizes)
            {
                if(sizePredicate != null && !sizePredicate(size))
                {
                    continue;
                }

                var sizeFilePath = image.FilePath(this._environment, size.Name);

                var magicImage = new ImageMagick.MagickImage();
                var readSettings = new ImageMagick.MagickReadSettings
                {
                    FrameIndex = frameIndex
                };

                if(isPDF)
                {
                    readSettings.Density = new ImageMagick.Density(300);
                }

                if (isVector)
                {
                    var tmpVector = new ImageMagick.MagickImage(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    var width = (size.Width ?? 400) * 2;

                    magicImage = new ImageMagick.MagickImage(ImageMagick.MagickColors.None, width, (int)(width * (tmpVector.Height / (double)tmpVector.Width)));
                    magicImage.ColorAlpha(ImageMagick.MagickColors.None);
                    magicImage.BackgroundColor = ImageMagick.MagickColors.None;
                    readSettings = new ImageMagick.MagickReadSettings
                    {
                        Density = new ImageMagick.Density(300),
                        BackgroundColor = ImageMagick.MagickColors.None
                    };
                }

                magicImage.Read(ms, readSettings);
                magicImage.AutoOrient();

                if (hasAlpha && size.AlphaMode == ImageAlphaMode.Retain)
                {
                    magicImage.HasAlpha = true;
                    magicImage.ColorAlpha(ImageMagick.MagickColors.None);
                    magicImage.BackgroundColor = ImageMagick.MagickColors.None;
                }
                else
                {
                    switch (size.AlphaMode)
                    {
                        case ImageAlphaMode.ClearToWhite:
                            magicImage.ColorAlpha(ImageMagick.MagickColors.White);
                            magicImage.BackgroundColor = ImageMagick.MagickColors.White;
                            break;
                        case ImageAlphaMode.ClearToBlack:
                            magicImage.ColorAlpha(ImageMagick.MagickColors.Black);
                            magicImage.BackgroundColor = ImageMagick.MagickColors.Black;
                            break;
                    }
                }

                if (size.AlphaMode != ImageAlphaMode.Retain)
                {
                    magicImage.Quality = 60;

                    if (isPDF)
                    {
                        magicImage.Quality = 90;
                    }
                }

                var thumbGeometry = new ImageMagick.MagickGeometry
                {
                    Width = size.Width ?? 0,
                    Height = size.Height ?? 0,
                    FillArea = size.SizeMode switch
                    {
                        ImageSizeMode.KeepAspectRatio => false,
                        ImageSizeMode.Contain => false,
                        ImageSizeMode.Cover => true,
                        _ => throw new NotImplementedException()
                    },
                    Greater = size.SizeMode switch
                    {
                        ImageSizeMode.KeepAspectRatio => true,
                        _ => false
                    },
                    Less = size.SizeMode switch
                    {
                        _ => false
                    },
                };

                magicImage.Thumbnail(thumbGeometry);

                if (size.SizeMode != ImageSizeMode.KeepAspectRatio && size.Width > 0 && size.Height > 0)
                {
                    magicImage.Extent(size.Width.Value, size.Height.Value, ImageMagick.Gravity.Center);

                    if (thumbGeometry.FillArea)
                    {
                        magicImage.Crop(size.Width.Value, size.Height.Value, ImageMagick.Gravity.Center);
                    }
                }

                if(magicImage.Format == ImageMagick.MagickFormat.Pdf)
                {
                    magicImage.TransformColorSpace(ImageMagick.ColorProfile.USWebCoatedSWOP, ImageMagick.ColorProfile.SRGB);
                }

                magicImage.Write(sizeFilePath);
            }
        }
    }
}
