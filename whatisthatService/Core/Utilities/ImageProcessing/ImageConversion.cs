using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using AForge.Imaging.Filters;

namespace whatisthatService.Core.Utilities.ImageProcessing
{
    public class ImageConversion
    {
        public static Bitmap GetImageCrop(Bitmap image, Rectangle cropArea)
        {
            var cropper = new Crop(cropArea);
            return cropper.Apply(image);
        }

        public static byte[] ImageToPngByteArray(Bitmap image)
        {
            using (var ms = new MemoryStream())
            {
                // Convert Image to byte[]
                const String codecName = "image/png";
                var imageCodecInfo = GetImageCodeInfo(codecName);
                var encoder = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(encoder, 100L);
                image.Save(ms, imageCodecInfo, encoderParameters);
                return ms.ToArray();
            }
        }

        public static Image ByteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }

        public static Bitmap ResizeImageIfNeeded(Bitmap image, int minSize, int maxSize)
        {
            try
            {
                Double minDimension = Math.Min(image.Size.Width, image.Size.Height);
                Double maxDimension = Math.Max(image.Size.Width, image.Size.Height);
                var minRatio = minSize / minDimension;
                var maxRatio = maxSize / maxDimension;
                if (maxRatio < 1.0)//downsample to MAX_SIZE
                {
                    var newWidth = Convert.ToInt32(Math.Floor(maxRatio * image.Size.Width));
                    var newHeight = Convert.ToInt32(Math.Floor(maxRatio * image.Size.Height));
                    return ResizeImage(image, newWidth, newHeight);
                }
                if (minRatio > 1.0)//upsample to MIN_SIZE
                {
                    var newWidth = Convert.ToInt32(Math.Ceiling(minRatio * image.Size.Width));
                    var newHeight = Convert.ToInt32(Math.Ceiling(minRatio * image.Size.Height));
                    var newImage = ResizeImage(image, newWidth, newHeight);
                    //Improve image quality with respect to image recognition before returning:
                    if (minRatio > 2)
                    {
                        return CleanUpUpscaledImage(newImage);
                    }
                    return newImage;
                }
                return image;
            }
            catch (Exception e)
            {
                var message = "Failed to resize image: " + e.Message;
                throw new ApplicationException(message, e);
            }
        }

        public static Bitmap CleanUpUpscaledImage(Bitmap image)
        {
            var blurFilter = new GaussianBlur {Sigma = 5.0};
            return blurFilter.Apply(image);
        }

        public static Bitmap ResizeImage(Bitmap originalImage, Int32 newWidth, Int32 newHeight)
        {
            var resizer = new ResizeBicubic(newWidth, newHeight);
            return resizer.Apply(originalImage);
        }

        public static Bitmap ConvertTo24BitColorBitmap(Image image)
        {
           return  AForge.Imaging.Image.Clone(new Bitmap(image), PixelFormat.Format24bppRgb);
        }

        private static ImageCodecInfo GetImageCodeInfo(String mimeType)
        {
            var encoders = ImageCodecInfo.GetImageEncoders();
            return encoders.FirstOrDefault(encoder => encoder.MimeType == mimeType);
        }
    }
}
