using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Caching;
using System.Text.RegularExpressions;

namespace whatisthatService.SpeciesIndentifier.Utilities.Caching
{
    ///<summary>Thread safe image cache that uses memory for the application duration, and disk storage for long term caching.
    ///</summary>
    public class LongTermImageCache
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object FileLock = new object();
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly CacheItemPolicy _cacheItemPolicy = new CacheItemPolicy();
        private readonly DirectoryInfo _fileSaveDirectory;
        private readonly Image _nullImage = new Bitmap(10,10);

        public LongTermImageCache(DirectoryInfo fileSaveDirectory)
        {
            _fileSaveDirectory = new DirectoryInfo(fileSaveDirectory.FullName);

            lock (FileLock)
            {
                if (!_fileSaveDirectory.Exists)
                {
                    _fileSaveDirectory.Create();
                }
            }
        }

        public Image this[string key]
        {
            get { return (Image)_cache[key]; }
            set { _cache[key] = value; }
        }

        public Image Get(String key)
        {
            var cachedValue = (Image)_cache.Get(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var image = ReadFromFile(key);
            Set(key, image, false);
            return image;
        }

        private Image ReadFromFile(String key)
        {
            var fileName = GenerateFileSavePath(key);

            lock (FileLock)
            {
                return !File.Exists(fileName) ? null : Image.FromFile(fileName);
            }
        }

        public void Set(String key, Image image)
        {
            Set(key, image, true);
        }

        private void Set(String key, Image image, Boolean writeToFile)
        {
            var finalImage = image ?? _nullImage;
            _cache.Set(key, finalImage, _cacheItemPolicy);

            if (!writeToFile)
            {
                return;
            }

            WriteToFile(key, finalImage);
        }

        private void WriteToFile(String key, Image image)
        {
            lock (FileLock)
            {
                try
                {
                    var fileName = GenerateFileSavePath(key);
                    image.Save(fileName, ImageFormat.Png);
                }
                catch (Exception e)
                {
                    var message = "Failed to write cache to file: " + e.Message;
                    throw new ApplicationException(message);
                } 
            }
        }

        private String GenerateFileSavePath(String key)
        {
            var sanitizedKey = SanitizeForFilePathName(key);
            return _fileSaveDirectory.FullName + "/" + sanitizedKey + ".png";
        }

        private String SanitizeForFilePathName(String value)
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(value, "");
        }
    }
}