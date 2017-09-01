using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using Scree.Cache;
using Scree.Core.IoC;

namespace Scree.Syn.Client
{
    internal enum GetStatus
    {
        Init = 0,
        Getting = 1,
    }

    public interface ISynClientService : IService
    {
        void Add(ISynable synData);

        bool IsLazy<T>();

        bool Peek<T>(string id, out long version) where T : ISynable;
    }
}
