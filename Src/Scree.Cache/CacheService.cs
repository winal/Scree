using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading;
using Scree.Log;
using Scree.Core.IoC;

namespace Scree.Cache
{
    public sealed class CacheService : ServiceBase, ICacheService
    {
        private static readonly int CacheClearInterval = 600 * 1000;//缓存清理间隔，单位ms，默认600s
        private static readonly int MinStayTime = 90;//缓存最短驻留时间，以最后获取时间计算，单位s，默认90s
        private static readonly int WriteLockTimeout = 5 * 1000;//获取更新锁超时时间，单位ms，默认5000ms
        private static readonly int FixLoggingInterval = 10;//固定缓存尺寸日志记录时间间隔，单位M，默认10M

        private const string CACHECLEARERROR = "Cache clear error";
        private const string SIZEOVER = "{0} cache size is over {1}, clear sum is {2}";
        private const string BEFOREOVERSIZECACHECLEARINFO = "Before OverSizeCacheClear(), {0} current cache size is {1}";
        private const string AFTEROVERSIZECACHECLEARINFO = "After OverSizeCacheClear(), {0} current cache size is {1}";
        private const string BEFOREOVERDUETIMECACHECLEARINFO = "Before OverdueTimeCacheClear(), {0} current cache size is {1}";
        private const string AFTEROVERDUETIMECACHECLEAR = "After OverdueTimeCacheClear(), {0} current cache size is {1}";
        private const string CACHEISFIX = "{0} cache is fix, current size is {1}";

        private static readonly Dictionary<string, Dictionary<string, MyCacheObject>> Types = new Dictionary<string, Dictionary<string, MyCacheObject>>(29);
        private static readonly object LockCacheClear = new object();
        private static readonly object LockTypes = new object();

        public override bool Init()
        {
            return true;
        }

        static CacheService()
        {
            try
            {
                if (CacheCommConfig.ClearInterval > 0)
                {
                    CacheClearInterval = CacheCommConfig.ClearInterval * 1000;
                }

                if (CacheCommConfig.MinStayTime > 0)
                {
                    MinStayTime = CacheCommConfig.MinStayTime;
                }

                if (CacheCommConfig.WriteLockTimeout > 0)
                {
                    WriteLockTimeout = CacheCommConfig.WriteLockTimeout;
                }

                if (CacheCommConfig.FixLoggingInterval > 0)
                {
                    FixLoggingInterval = CacheCommConfig.FixLoggingInterval;
                }

                Thread t = new Thread(CacheClearThreadMethod);
                t.Start();
            }
            catch (Exception ex)
            {
                LogProxy.Fatal(ex, true);
            }
        }

        public bool IsNeedCached<T>() where T : ICacheable
        {
            return SingleTypeCacheConfig.IsExist<T>();
        }

        public bool IsNeedCached(string typeName)
        {
            return SingleTypeCacheConfig.IsExist(typeName);
        }

        public bool IsCached(string typeName, string id)
        {
            if (!IsNeedCached(typeName))
            {
                return false;
            }

            id = id.ToLower();
            Dictionary<string, MyCacheObject> caches = GetCaches(typeName);
            return caches.ContainsKey(id);
            //非精确判定缓存，过期缓存也计算在内，不过也没必要精确。
        }

        public bool IsCached<T>(string id) where T : ICacheable
        {
            return IsCached(typeof(T).FullName, id);
        }

        public void AddEvent<T>(OnCacheCleared cacheCleared, OnCacheSetted cacheSetted, OnCacheGetted cacheGetted) where T : ICacheable
        {
            if (!IsNeedCached<T>())
            {
                return;
            }

            SingleTypeCacheConfig config = SingleTypeCacheConfig.Get<T>();
            if (cacheCleared != null)
            {
                config.CacheCleared += cacheCleared;
            }
            if (cacheSetted != null)
            {
                config.CacheSetted += cacheSetted;
            }
            if (cacheGetted != null)
            {
                config.CacheGetted += cacheGetted;
            }
        }

        private static Dictionary<string, MyCacheObject> GetCaches(string typeName)
        {
            if (Types.ContainsKey(typeName))
            {
                return Types[typeName];
            }

            lock (LockTypes)
            {
                if (Types.ContainsKey(typeName))
                {
                    return Types[typeName];
                }

                Dictionary<string, MyCacheObject> caches = new Dictionary<string, MyCacheObject>(29);
                Types.Add(typeName, caches);

                return caches;
            }
        }

        public void Set(ICacheable obj)
        {
            if (obj == null)
            {
                return;
            }

            string fullName = obj.GetType().FullName;
            if (!IsNeedCached(fullName))
            {
                return;
            }

            SingleTypeCacheConfig config = SingleTypeCacheConfig.Get(fullName);
            Dictionary<string, MyCacheObject> caches = GetCaches(fullName);

            if (!config.LockObj.TryEnterWriteLock(WriteLockTimeout))
            {
                return;
            }

            try
            {
                string id = obj.Id.ToLower();
                MyCacheObject myCacheObject;
                if (caches.ContainsKey(id))
                {
                    myCacheObject = caches[id];
                    if (myCacheObject.Obj.Version >= obj.Version)
                    {
                        return;
                    }

                    myCacheObject.Obj = obj;
                    myCacheObject.OverdueTime = DateTime.Now.AddSeconds(config.Second);
                }
                else
                {
                    myCacheObject = new MyCacheObject(obj, DateTime.Now.AddSeconds(config.Second));
                    caches.Add(id, myCacheObject);
                }

                try
                {
                    config.Setted(obj);
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }

            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
            finally
            {
                config.LockObj.ExitWriteLock();
            }
        }

        public ICacheable Get<T>(string id) where T : ICacheable
        {
            return Get(typeof(T), id);
        }
        public ICacheable Get(Type type, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            string fullName = type.FullName;
            if (!IsNeedCached(fullName))
            {
                return null;
            }

            SingleTypeCacheConfig config = SingleTypeCacheConfig.Get(fullName);

            Dictionary<string, MyCacheObject> caches = GetCaches(fullName);

            config.LockObj.EnterReadLock();

            try
            {
                id = id.ToLower();
                if (!caches.ContainsKey(id))
                {
                    return null;
                }

                MyCacheObject myCacheObject = caches[id];

                if (!config.IsFix && myCacheObject.OverdueTime < DateTime.Now)
                {
                    return null;
                }

                myCacheObject.LastGetTime = DateTime.Now;

                try
                {
                    config.Getted(myCacheObject.Obj);
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }

                return myCacheObject.Obj;
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
            finally
            {
                config.LockObj.ExitReadLock();
            }

            return null;
        }

        #region 清理缓存
        private static string[] GetTypesKeys()
        {
            lock (LockTypes)
            {
                return Types.Keys.ToArray();
            }
        }

        private static void CacheClearThreadMethod()
        {
            while (true)
            {
                Thread.Sleep(CacheClearInterval);

                CacheClear();
            }
        }

        private static void CacheClear()
        {
            string[] typesKeys = GetTypesKeys();

            foreach (string typeName in typesKeys)
            {
                SingleTypeCacheConfig config = SingleTypeCacheConfig.Get(typeName);
                Dictionary<string, MyCacheObject> caches = GetCaches(typeName);

                #region 固定缓存
                if (config.IsFix)
                {
                    if (config.NextFixLoggingTime < DateTime.Now)
                    {
                        config.NextFixLoggingTime = DateTime.Now.AddMinutes(FixLoggingInterval);
                        LogProxy.InfoFormat(CACHEISFIX, typeName, caches.Count);
                    }

                    continue;
                }
                #endregion

                OverdueTimeCacheClear(caches, typeName, config);

                if (config.Size > 0)
                {
                    OverSizeCacheClear(caches, typeName, config);
                }
            }
        }

        private static string[] GetOverdueTimeKeys(Dictionary<string, MyCacheObject> caches, SingleTypeCacheConfig config)
        {
            config.LockObj.EnterReadLock();
            try
            {
                return (from o in caches
                        where o.Value.OverdueTime < DateTime.Now
                        select o.Key).ToArray();
            }
            finally
            {
                config.LockObj.ExitReadLock();
            }
        }

        private static void OverdueTimeCacheClear(Dictionary<string, MyCacheObject> caches, string typeName, SingleTypeCacheConfig config)
        {
            LogProxy.InfoFormat(BEFOREOVERDUETIMECACHECLEARINFO, typeName, caches.Count);

            string[] objKeys = GetOverdueTimeKeys(caches, config);
            if (objKeys == null || objKeys.Length == 0)
            {
                return;
            }
            CacheClear(caches, objKeys, ClearType.OverdueTime, config);

            LogProxy.InfoFormat(AFTEROVERDUETIMECACHECLEAR, typeName, caches.Count);
        }

        private static string[] GetOverSizeKeys(Dictionary<string, MyCacheObject> caches, int clearSum, SingleTypeCacheConfig config)
        {
            config.LockObj.EnterReadLock();
            try
            {
                return (from o in caches
                        where o.Value.LastGetTime.AddSeconds(MinStayTime) < DateTime.Now
                        orderby o.Value.LastGetTime ascending
                        select o.Key).Take(clearSum).ToArray();
            }
            finally
            {
                config.LockObj.ExitReadLock();
            }
        }

        private static void OverSizeCacheClear(Dictionary<string, MyCacheObject> caches, string typeName, SingleTypeCacheConfig config)
        {
            int cnt = caches.Count;
            LogProxy.InfoFormat(BEFOREOVERSIZECACHECLEARINFO, typeName, cnt);

            if (cnt < config.Size * 1.2)
            {
                return;
            }

            int clearSum = (int)(cnt - config.Size * 0.8);

            LogProxy.InfoFormat(SIZEOVER, typeName, cnt - config.Size, clearSum);

            string[] objKeys = GetOverSizeKeys(caches, clearSum, config);
            if (objKeys == null || objKeys.Length == 0)
            {
                return;
            }
            CacheClear(caches, objKeys, ClearType.OverSize, config);

            LogProxy.InfoFormat(AFTEROVERSIZECACHECLEARINFO, typeName, caches.Count);
        }

        private static void CacheClear(Dictionary<string, MyCacheObject> caches, string[] objKeys, ClearType clearType, SingleTypeCacheConfig config)
        {
            MyCacheObject objCache;
            if (!config.LockObj.TryEnterWriteLock(WriteLockTimeout))
            {
                return;
            }

            try
            {
                foreach (string objKey in objKeys)
                {
                    objCache = caches[objKey];
                    switch (clearType)
                    {
                        case ClearType.OverdueTime:
                            {
                                if (objCache.OverdueTime > DateTime.Now)
                                {
                                    break;
                                }

                                caches.Remove(objCache.Obj.Id);

                                try
                                {
                                    config.Cleared(objCache.Obj);
                                }
                                catch (Exception ex)
                                {
                                    LogProxy.Error(ex, false);
                                }

                                break;
                            }
                        case ClearType.OverSize:
                            {
                                if (objCache.LastGetTime.AddSeconds(MinStayTime) > DateTime.Now)
                                {
                                    break;
                                }

                                caches.Remove(objCache.Obj.Id);

                                try
                                {
                                    config.Cleared(objCache.Obj);
                                }
                                catch (Exception ex)
                                {
                                    LogProxy.Error(ex, false);
                                }
                                break;
                            }
                    }
                }
            }
            finally
            {
                config.LockObj.ExitWriteLock();
            }

        }
        #endregion

        private class MyCacheObject
        {
            public ICacheable Obj { get; set; }
            public DateTime OverdueTime { get; set; }
            public DateTime LastGetTime { get; set; }

            public MyCacheObject(ICacheable obj, DateTime overdueTime)
            {
                this.Obj = obj;

                if (obj is IFixCache && ((IFixCache)obj).IsFixCache)
                {
                    this.OverdueTime = DateTime.MaxValue;
                }
                else
                {
                    this.OverdueTime = overdueTime;
                }
            }
        }

        private enum ClearType
        {
            OverdueTime = 0,
            OverSize = 1
        }

        private enum CacheClearStatus
        {
            Init = 0,
            Cleaning = 1
        }
    }
}
