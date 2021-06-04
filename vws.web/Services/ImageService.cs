using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vws.web.Domain;

namespace vws.web.Services
{
    public class ImageService : IImageService
    {
        private ILogger _logger;
        private const int ImageMinimumBytes = 512;

        public ImageService(ILogger<IImageService> logger)
        {
            _logger = logger;
        }

        private Bitmap Resize(Bitmap imgPhoto, Size objSize, ImageFormat enuType)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;

            int destX = 0;
            int destY = 0;
            int destWidth = objSize.Width;
            int destHeight = objSize.Height;

            Bitmap bmPhoto;
            if (enuType == ImageFormat.Png)
                bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            else if (enuType == ImageFormat.Gif)
                bmPhoto = new Bitmap(destWidth, destHeight); //PixelFormat.Format8bppIndexed should be the right value for a GIF, but will throw an error with some GIF images so it's not safe to specify.
            else
                bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);

            //For some reason the resolution properties will be 96, even when the source image is different,
            //so this matching does not appear to be reliable.
            //bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            //If you want to override the default 96dpi resolution do it here
            //bmPhoto.SetResolution(72, 72);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        public void SaveInOtherQualities(Domain._file.File fileResponse)
        {
            var address = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + fileResponse.Address;
            Bitmap image = new Bitmap(address);

            ImageFormat format;
            using (Image img = Image.FromFile(address))
            {
                format = img.RawFormat;
            }

            int[] wantedSizes = { 100, 250, 500 };
            var minDimensionSize = Math.Min(image.Width, image.Height);

            if (minDimensionSize < wantedSizes.Min())
                return;

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}");
            string filePath = address;
            filePath = filePath.Replace(uploadPath, "");
            string[] subs = filePath.Split(Path.DirectorySeparatorChar);
            filePath = "";
            for (int i = 0; i < subs.Length - 1; i++)
            {
                filePath += subs[i];
                if (i != subs.Length - 2)
                    filePath += Path.DirectorySeparatorChar;
            }

            foreach (var size in wantedSizes)
            {
                if (minDimensionSize < size)
                    continue;
                var path = uploadPath + Path.DirectorySeparatorChar + filePath + Path.DirectorySeparatorChar + fileResponse.Id.ToString() + "-" + size.ToString() + "." + fileResponse.Extension;
                Bitmap final = Resize(image, new Size(size, size), format);
                final.Save(path);
            }

            image.Dispose();
        }

        public bool IsImage(IFormFile postedFile)
        {
            //  Check the image mime types
            if (postedFile.ContentType.ToLower() != "image/jpg" &&
                        postedFile.ContentType.ToLower() != "image/jpeg" &&
                        postedFile.ContentType.ToLower() != "image/pjpeg" &&
                        postedFile.ContentType.ToLower() != "image/gif" &&
                        postedFile.ContentType.ToLower() != "image/x-png" &&
                        postedFile.ContentType.ToLower() != "image/png")
            {
                _logger.LogInformation("IsImage : check the image mime types");
                return false;
            }

            //  Check the image extension
            if (Path.GetExtension(postedFile.FileName).ToLower() != ".jpg"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".png"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".jpeg")
            {
                _logger.LogInformation("IsImage : check image extension");
                return false;
            }

            //  Attempt to read the file and check the first bytes
            try
            {
                if (!postedFile.OpenReadStream().CanRead)
                {
                    _logger.LogInformation("IsImage : can read");
                    return false;
                }
                //check whether the image size exceeding the limit or not
                if (postedFile.Length < ImageMinimumBytes)
                {
                    _logger.LogInformation("IsImage : exceeding the limit");
                    return false;
                }

                byte[] buffer = new byte[ImageMinimumBytes];
                postedFile.OpenReadStream().Read(buffer, 0, ImageMinimumBytes);
                string content = System.Text.Encoding.UTF8.GetString(buffer);
                if (Regex.IsMatch(content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                {
                    _logger.LogInformation("IsImage : last if");
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            // Try to instantiate new Bitmap, if .NET will throw exception we can assume that it's not a valid image

            try
            {
                using (var bitmap = new Bitmap(postedFile.OpenReadStream()))
                {
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                return false;
            }
            finally
            {
                postedFile.OpenReadStream().Position = 0;
            }

            return true;
        }

        public bool IsImageSquare(IFormFile image)
        {
            using (var img = Image.FromStream(image.OpenReadStream()))
            {
                if (Math.Abs(img.Width - img.Height) <= 5)
                    return true;
                return false;
            }
        }
    }
}
