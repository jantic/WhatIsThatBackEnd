using System;
using System.Linq;
using System.Runtime.Caching;

namespace whatisthatService.Core.Utilities.Caching
{
    ///<summary>Thread safe generic cache for serialiable types that uses memory for the application duration, and disk storage for long term caching.
    ///</summary>
    //TODO: Reimplement using Redis
    public class GenericLongTermCache<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly CacheItemPolicy _cacheItemPolicy = new CacheItemPolicy();

        public GenericLongTermCache()
        {
            if (!typeof (T).GetCustomAttributes(typeof (SerializableAttribute), true).Any())
            {
                const string message = "Type must be serializable.";
                throw new ArgumentException(message);
            }
        }

        public T this[string key]
        {
            get { return (T) _cache[key]; }
            set { _cache[key] = value; }
        }

        public T Get(String key)
        {
            var cachedValue = (T) _cache.Get(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            return default(T);
        }

        public void Set(String key, T value)
        {
            _cache.Set(key, value, _cacheItemPolicy);
        }
    }
}