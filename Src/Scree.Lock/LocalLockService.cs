using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Scree.Log;

namespace Scree.Lock
{
    public interface ILockable
    {
        string Id { get; }
    }

    public sealed class LocalLockService<T>
        where T : class, ILockable
    {
        private static readonly Dictionary<string, LockItem> Locks = new Dictionary<string, LockItem>(919);//id, LockItem
        private static readonly object LockObj = new object();
        private static readonly int LOCKEREXPIREINTERVAL = 60;//锁过期时间，单位s，默认60s
        private static readonly int TRYINTERVAL = 3 * 1000;//获取失败时，重试间隔时间，单位ms，默认3s
        private static readonly int TRYCOUNT = 5;//获取失败时，重试次数，默认3

        static LocalLockService()
        {
            if (LocalLockConfig.Expire > 0)
            {
                LOCKEREXPIREINTERVAL = LocalLockConfig.Expire;
            }

            if (LocalLockConfig.TryInterval > 0)
            {
                TRYINTERVAL = LocalLockConfig.TryInterval * 1000;
            }

            if (LocalLockConfig.Trys > 0)
            {
                TRYCOUNT = LocalLockConfig.Trys;
            }
        }

        public static bool GetLock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            id = id.ToLower().Trim();

            int wait = 0;
            int threadId = Thread.CurrentThread.ManagedThreadId;

        Next:
            lock (LockObj)
            {
                if (Locks.ContainsKey(id))
                {
                    if (Locks[id].ThreadId == threadId)
                    {
                        Locks[id].ReLock();
                        return true;
                    }

                    if (Locks[id].IsOverdue())
                    {//已经过期
                        Locks[id] = new LockItem(threadId);
                        return true;
                    }
                }
                else
                {
                    Locks.Add(id, new LockItem(threadId));
                    return true;
                }
            }

            if (wait < TRYCOUNT)
            {
                wait++;
                Thread.Sleep(TRYINTERVAL);
                goto Next;
            }

            return false;
        }

        public static void ReleaseLock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            try
            {
                id = id.ToLower().Trim();
                int threadId = Thread.CurrentThread.ManagedThreadId;

                if (!Locks.ContainsKey(id))
                {
                    return;
                }

                lock (LockObj)
                {
                    if (!Locks.ContainsKey(id) || Locks[id].ThreadId != threadId)
                    {
                        return;
                    }

                    Locks[id].Release();
                    if (!Locks[id].IsLocked())
                    {
                        Locks.Remove(id);
                    }
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private class LockItem
        {
            public int ThreadId { get; private set; }
            public int LockSum { get; private set; }
            public DateTime OverdueTime { get; private set; }

            public LockItem(int threadId)
            {
                this.ThreadId = threadId;
                this.OverdueTime = DateTime.Now.AddSeconds(LOCKEREXPIREINTERVAL); ;
                LockSum = 1;
            }

            public void ReLock()
            {
                LockSum++;
                this.OverdueTime = DateTime.Now.AddSeconds(LOCKEREXPIREINTERVAL);
            }

            public void Release()
            {
                LockSum--;
            }

            public bool IsOverdue()
            {
                return this.OverdueTime < DateTime.Now;
            }

            public bool IsLocked()
            {
                return this.LockSum > 0;
            }
        }

    }
}
