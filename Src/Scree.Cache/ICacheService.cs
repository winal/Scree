using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading;
using Scree.Core.IoC;

namespace Scree.Cache
{
    public delegate void OnCacheCleared(ICacheable obj);
    public delegate void OnCacheSetted(ICacheable obj);
    public delegate void OnCacheGetted(ICacheable obj);

    public interface ICacheService : IService
    {
        bool IsNeedCached<T>() where T : ICacheable;

        bool IsNeedCached(string typeName);

        bool IsCached(string typeName, string id);

        bool IsCached<T>(string id) where T : ICacheable;

        void AddEvent<T>(OnCacheCleared cacheCleared, OnCacheSetted cacheSetted, OnCacheGetted cacheGetted) where T : ICacheable;

        //void Set<T>(T obj) where T : ICacheable;
        void Set(ICacheable obj);

        ICacheable Get<T>(string id) where T : ICacheable;
        ICacheable Get(Type type, string id);
    }
}
