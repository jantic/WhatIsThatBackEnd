using System;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace whatisthatService.SpeciesIndentifier.Utilities.Caching
{
    ///<summary>Thread safe generic cache for serialiable types that uses memory for the application duration, and disk storage for long term caching.
    ///</summary>
    public class GenericLongTermCache<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object FileLock = new object();
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly CacheItemPolicy _cacheItemPolicy = new CacheItemPolicy();
        private readonly DirectoryInfo _fileSaveDirectory;

        public GenericLongTermCache(DirectoryInfo fileSaveDirectory)
        {
            if (!typeof (T).GetCustomAttributes(typeof (SerializableAttribute), true).Any())
            {
                const string message = "Type must be serializable.";
                throw new ArgumentException(message);
            }

            _fileSaveDirectory = new DirectoryInfo(fileSaveDirectory.FullName);

            lock (FileLock)
            {
                if (!_fileSaveDirectory.Exists)
                {
                    _fileSaveDirectory.Create();
                }
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

            var value = ReadFromFile(key);

            if (value != null)
            {
                Set(key, value, false);
            }

            return value;
        }

        private T ReadFromFile(String key)
        {
            var fileName = GenerateFileSavePath(key, typeof(T), "xml");

            lock (FileLock)
            {
                if (!File.Exists(fileName))
                {
                    return default(T);
                }

                var reader = new XmlSerializer(typeof(T));

                using (var file = new StreamReader(fileName))
                {
                    return (T) reader.Deserialize(file);
                }
            }
        }

        public void Set(String key, T value)
        {
            Set(key, value, true);
        }

        private void Set(String key, T value, Boolean writeToFile)
        {
            _cache.Set(key, value, _cacheItemPolicy);

            if (!writeToFile)
            {
                return;
            }

            WriteToFile(key, value);
        }

        private void WriteToFile(String key, T value)
        {
            var fileName = GenerateFileSavePath(key, value.GetType(), "xml");


            lock (FileLock)
            {
                try
                {
                    var writer = new XmlSerializer(typeof (T));

                    using (var file = new StreamWriter(fileName))
                    {
                        writer.Serialize(file, value);
                    }
                }
                catch (Exception e)
                {
                    var message = "Failed to write cache to file: " + e.Message;
                    throw new ApplicationException(message);
                }
            }
        }

        private String GenerateFileSavePath(String key, Type type, String extension)
        {
            var sanitizedKey = SanitizeForFilePathName(key);
            return _fileSaveDirectory.FullName + "/" + type.Name + "_" + sanitizedKey + "." + extension;
        }

        private String SanitizeForFilePathName(String value)
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(value, "");
        }
    }
}