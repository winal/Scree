using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scree.Cache
{
    public interface ICacheable
    {
        string Id { get; }
        long Version { get; }
    }

    public interface IFixCache
    {
        bool IsFixCache { get; }
    }
    public class ArrayCache<T> : ICacheable
    {
        public string Id { get; set; }
        public long Version { get; set; }
        public T[] Objects { get; set; }
    }

}
