using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Image File Service
    /// </summary>
    public class ImageFileServiceEF : IImageFileService
    {
        /// <summary>
        /// Add Image File
        /// </summary>
        /// <param name="file"></param>
        /// <param name="stream"></param>
        /// <param name="originalFileNameForStreams"></param>
        /// <param name="imageFolderName"></param>
        /// <param name="isImage"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> Add(IFormFile file, Stream stream, string originalFileNameForStreams, string imageFolderName, bool isImage = true, string contentType = "image/jpeg")
        {
            RServiceResult<RImage>
                pictureFile =
                await ProcessImage
                (
                    file,
                    new RImage()
                    {
                        DataTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        FolderName = string.IsNullOrEmpty(imageFolderName) ? DateTime.Now.ToString("yyyy-MM") : imageFolderName
                    },
                    stream,
                    originalFileNameForStreams,
                    isImage,
                    contentType
                    );
            if (pictureFile == null)
                return new RServiceResult<RImage>(null, pictureFile.ExceptionString);

            return new RServiceResult<RImage>(pictureFile.Result);
        }

        /// <summary>
        /// store added image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> Store(RImage image)
        {
            _context.GeneralImages.Add(image);
            await _context.SaveChangesAsync();

            return new RServiceResult<RImage>(image);
        }

        /// <summary>
        /// Image extensions this service will store, mapped to the Content-Type that will always be
        /// used both when writing RImage.ContentType and when the file is later served back (see
        /// RImageControllerBase._Get) - never the caller-supplied Content-Type or the "contentType"
        /// parameter below, and never a client's claimed file extension by itself. Deliberately does
        /// not include .svg: an SVG is XML and can carry an embedded &lt;script&gt;, so unlike a
        /// bitmap format it is not safe to treat as "just an image" even though browsers render it as
        /// one.
        /// </summary>
        private static readonly Dictionary<string, string> _allowedImageContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
        };

        private async Task<RServiceResult<RImage>> ProcessImage(IFormFile uploadedImage, RImage pictureFile, Stream stream, string originalFileNameForStreams, bool isImage, string contentType)
        {
            if (uploadedImage == null && stream == null)
            {
                return new RServiceResult<RImage>(null, "ProcessImage: uploadedImage == null && stream == null");
            }

            pictureFile.FileSizeInBytes = uploadedImage == null ? stream.Length : uploadedImage.Length;
            pictureFile.OriginalFileName = uploadedImage == null ? originalFileNameForStreams : uploadedImage.FileName;

            // Read the whole upload into memory once, so it can be validated - extension allow-list,
            // then actually decoding as an image - BEFORE anything is written to permanent storage.
            // The previous code wrote the file to its final path first and only decoded it afterward,
            // and only when isImage was true; isImage's caller-supplied value (see the public
            // UploadImage endpoint) let it be set to false to skip that check entirely, so a caller of
            // that endpoint could upload literally any file and later have it served back with
            // whatever Content-Type it claimed at upload time - a stored-XSS delivery vector this
            // closes by validating unconditionally, regardless of isImage.
            byte[] fileBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                if (uploadedImage != null)
                    await uploadedImage.CopyToAsync(ms);
                else
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(ms);
                }
                fileBytes = ms.ToArray();
            }

            string ext = uploadedImage != null ? Path.GetExtension(uploadedImage.FileName).ToLower() : !string.IsNullOrEmpty(originalFileNameForStreams) ? Path.GetExtension(originalFileNameForStreams).ToLower() : ".jpg";
            if (ext == ".jpeg")
            {
                ext = ".jpg";
            }

            if (!_allowedImageContentTypes.TryGetValue(ext, out string resolvedContentType))
            {
                return new RServiceResult<RImage>(null, $"پسوند فایل «{ext}» برای تصویر مجاز نیست.");
            }

            pictureFile.ContentType = resolvedContentType;

            // Confirms fileBytes actually decodes as that kind of image - an allow-listed extension is
            // not by itself proof the bytes behind it really are an image (isImage, below, only
            // controls whether width/height metadata is additionally extracted from the same decode).
            try
            {
                using (MemoryStream verifyStream = new MemoryStream(fileBytes))
                using (Image img = Image.FromStream(verifyStream))
                {
                    if (isImage)
                    {
                        pictureFile.ImageWidth = img.Width;
                        pictureFile.ImageHeight = img.Height;
                    }
                }
            }
            catch
            {
                return new RServiceResult<RImage>(null, "فایل ارسال شده یک تصویر معتبر نیست.");
            }

            string fullDirStorePath = Path.Combine(ImageStoragePath, pictureFile.FolderName);

            if (!Directory.Exists(fullDirStorePath))
            {
                try
                {
                    Directory.CreateDirectory(fullDirStorePath);
                }
                catch
                {
                    return new RServiceResult<RImage>(null, $"ProcessImage: create dir failed {fullDirStorePath}");
                }
            }

            pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + ext;

            string originalFileStorePath = Path.Combine(fullDirStorePath, pictureFile.StoredFileName);
            while (File.Exists(originalFileStorePath))
            {
                pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + "-" + Guid.NewGuid().ToString() + ext;
                originalFileStorePath = Path.Combine(fullDirStorePath, pictureFile.StoredFileName);
            }

            await File.WriteAllBytesAsync(originalFileStorePath, fileBytes);

            return new RServiceResult<RImage>(pictureFile);
        }


        /// <summary>
        /// returns image info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> GetImage(Guid id)
        {
            return new RServiceResult<RImage>(
                await _context.GeneralImages.AsNoTracking()
                     .Where(p => p.Id == id)
                     .SingleOrDefaultAsync()
                     );
        }

        /// <summary>
        /// delete image (from database and file system)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteImage(Guid id)
        {
            RServiceResult<RImage> img = await GetImage(id);
            if (!string.IsNullOrEmpty(img.ExceptionString))
                return new RServiceResult<bool>(false, img.ExceptionString);
            if (img.Result == null)
            {
                return new RServiceResult<bool>(false, "image not found");
            }
            File.Delete(GetImagePath(img.Result).Result);
            _context.GeneralImages.Remove(img.Result);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Get Image Storage Path
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public RServiceResult<string> GetImagePath(RImage image)
        {
            return new RServiceResult<string>(Path.Combine(ImageStoragePath, image.FolderName, image.StoredFileName));
        }

        /// <summary>
        /// Image Storage Path
        /// </summary>
        public string ImageStoragePath { get { return $"{Configuration.GetSection("PictureFileService")["StoragePath"]}"; } }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public ImageFileServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context, IConfiguration configuration)
        {
            _context = context;
            Configuration = configuration;
        }

    }
}
