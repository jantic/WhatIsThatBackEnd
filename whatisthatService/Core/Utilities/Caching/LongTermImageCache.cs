using System;
using System.Drawing;
using System.Runtime.Caching;

namespace whatisthatService.Core.Utilities.Caching
{
    ///<summary>Thread safe image cache that uses memory for the application duration, and disk storage for long term caching.
    ///</summary>
    //TODO: Reimplement using Redis
    public class LongTermImageCache
    {
        // ReSharper disable once StaticMemberInGenericType
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly CacheItemPolicy _cacheItemPolicy = new CacheItemPolicy();
        private readonly Image _nullImage = new Bitmap(10,10);

        public Image this[string key]
        {
            get { return (Image)_cache[key]; }
            set { _cache[key] = value; }
        }

        public Image Get(String key)
        {
            return (Image)_cache.Get(key);
        }


        public void Set(String key, Image image)
        {
            Set(key, image, true);
        }

        private void Set(String key, Image image, Boolean writeToFile)
        {
            var finalImage = image ?? _nullImage;
            _cache.Set(key, finalImage, _cacheItemPolicy);
        }
    }
}