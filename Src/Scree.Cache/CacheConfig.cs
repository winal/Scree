using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Log;
using System.Xml.Linq;
using System.Threading;

namespace Scree.Cache
{
    internal static class CacheCommConfig
    {
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\cache.config";

        public static int ClearInterval
        {
            get
            {
                return _clearInterval;
            }
        }
        private static readonly int _clearInterval;

        public static int MinStayTime
        {
            get
            {
                return _minStayTime;
            }
        }
        private static readonly int _minStayTime;

        public static int WriteLockTimeout
        {
            get
            {
                return _writeLockTimeout;
            }
        }
        private static readonly int _writeLockTimeout;

        public static int FixLoggingInterval
        {
            get
            {
                return _fixLoggingInterval;
            }
        }
        private static readonly int _fixLoggingInterval;

        static CacheCommConfig()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);
                XElement localLock = root.Element("Comm");
                int.TryParse(localLock.Element("ClearInterval").Value, out _clearInterval);
                int.TryParse(localLock.Element("MinStayTime").Value, out _minStayTime);
                int.TryParse(localLock.Element("WriteLockTimeout").Value, out _writeLockTimeout);
                int.TryParse(localLock.Element("FixLoggingInterval").Value, out _fixLoggingInterval);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }

    internal sealed class SingleTypeCacheConfig
    {
        private const int DEFAULTCACHESECONDS = 300;
        private const int DEFAULTCACHESIZE = 100;
        private const string CACHETYPENAMEISNULL = "Cache type name is null";

        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\cache.config";
        private static readonly Dictionary<string, SingleTypeCacheConfig> MyCacheConfig = new Dictionary<string, SingleTypeCacheConfig>(29);

        public ReaderWriterLockSlim LockObj = new ReaderWriterLockSlim();
        public event OnCacheCleared CacheCleared;
        public event OnCacheSetted CacheSetted;
        public event OnCacheGetted CacheGetted;

        public void Cleared(ICacheable obj)
        {
            if (CacheCleared != null)
            {
                CacheCleared(obj);
            }
        }
        public void Setted(ICacheable obj)
        {
            if (CacheSetted != null)
            {
                CacheSetted(obj);
            }
        }
        public void Getted(ICacheable obj)
        {
            if (CacheGetted != null)
            {
                CacheGetted(obj);
            }
        }

        public string Name { get; set; }
        public int Second { get; set; }
        public int Size { get; set; }
        public bool IsFix { get; set; }
        public DateTime NextFixLoggingTime { get; set; }

        private SingleTypeCacheConfig(string name, bool isFix, int second, int size)
        {
            this.Name = name;
            this.Second = second;
            this.Size = size;
            this.IsFix = isFix;
        }

        static SingleTypeCacheConfig()
        {
            try
            {
                XElement root = XElement.Load(ConfigDirectory);

                IEnumerable<XElement> types = root.Element("Types").Elements("Type");
                LoadSingleTypeCacheConfig(types);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void LoadSingleTypeCacheConfig(IEnumerable<XElement> types)
        {
            SingleTypeCacheConfig config;
            foreach (XElement el in types)
            {
                try
                {
                    string name = el.Attribute("name").Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        LogProxy.Warn(CACHETYPENAMEISNULL);
                        continue;
                    }

                    bool isFix = false;
                    if (el.Attribute("isfix") != null)
                    {
                        bool.TryParse(el.Attribute("isfix").Value.Trim(), out isFix);

                        if (isFix)
                        {
                            config = new SingleTypeCacheConfig(name, true,   0, 0);
                            MyCacheConfig[name] = config;

                            continue;
                        }
                    }

                    int second = 0;
                    if (el.Attribute("second") != null)
                    {
                        int.TryParse(el.Attribute("second").Value.Trim(), out second);

                        if (second < 0)
                        {
                            second = 0;
                        }
                    }

                    int size = 0;
                    if (el.Attribute("size") != null)
                    {
                        int.TryParse(el.Attribute("size").Value.Trim(), out size);

                        if (size < 0)
                        {
                            size = 0;
                        }
                    }

                    config = new SingleTypeCacheConfig(name, false,  second, size);
                    MyCacheConfig[name] = config;
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }

        }

        public static bool IsExist(string typeName)
        {
            return MyCacheConfig.ContainsKey(typeName);
        }

        public static bool IsExist<T>()
        {
            return IsExist(typeof(T).FullName);
        }

        public static SingleTypeCacheConfig Get(string typeName)
        {
            if (IsExist(typeName))
            {
                return MyCacheConfig[typeName];
            }
            return null;
        }

        public static SingleTypeCacheConfig Get<T>()
        {
            return Get(typeof(T).FullName);
        }

    }
}
