using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Scree.Log;
using Scree.Core.IoC;

namespace Scree.Lock
{
    public interface ILockService : IService
    {
        bool GetLock(ILockItem[] items, out string lockId);

        void ReleaseLock(string lockId);

        ILockItem CreateLockItem<T>(string id);
    }
}
