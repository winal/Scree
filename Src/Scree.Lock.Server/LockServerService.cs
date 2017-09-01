using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Scree.Log;
using Scree.Core.IoC;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

namespace Scree.Lock.Server
{
    public sealed class LockServerService : ServiceBase, ILockService
    {
        private static readonly Dictionary<string, ILockItem[]> MyLocks = new Dictionary<string, ILockItem[]>(919);//LockId, ILockItem[]
        private static readonly Dictionary<string, DateTime> LockExpireTime = new Dictionary<string, DateTime>(919);//LockId, DateTime(过期时间)
        private static readonly Dictionary<ILockItem, string> LockItems = new Dictionary<ILockItem, string>(1931);//LockItem, LockId
        private static readonly object LockObj = new object();
        private static readonly string ApplicationName = "LockServerService";//Remoting ApplicationName
        private static readonly int Port = 80;//Remoting 端口
        private static readonly int LOCKEREXPIREINTERVAL = 90;//锁过期时间，单位s，默认90s
        private static readonly int TRYINTERVAL = 5 * 1000;//锁获取失败时，重试间隔时间，单位ms，默认5s
        private static readonly int TRYCOUNT = 3;//获取失败时，重试次数，默认3
        private static readonly int LOCKECLEARINTERVAL = 30 * 1000;//过期未释放锁清理时间间隔，单位ms，默认30s

        private const string OVERDUELOCKSIZE = "The size of overdue lock is {0}";

        public override bool Init()
        {
            TcpChannel channel = new TcpChannel(Port);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(LockServerService), ApplicationName, WellKnownObjectMode.Singleton);

            Thread t = new Thread(LockClearThreadMethod);
            t.Start();

            return true;
        }

        static LockServerService()
        {
            try
            {
                ApplicationName = LockServerConfig.ApplicationName;

                if (LockServerConfig.Port > 0)
                {
                    Port = LockServerConfig.Port;
                }

                if (LockServerConfig.Expire > 0)
                {
                    LOCKEREXPIREINTERVAL = LockServerConfig.Expire;
                }

                if (LockServerConfig.TryInterval > 0)
                {
                    TRYINTERVAL = LockServerConfig.TryInterval * 1000;
                }

                if (LockServerConfig.Trys > 0)
                {
                    TRYCOUNT = LockServerConfig.Trys;
                }

                if (LockServerConfig.ClearInterval > 0)
                {
                    LOCKECLEARINTERVAL = LockServerConfig.ClearInterval * 1000;
                }
            }
            catch (Exception ex)
            {
                LogProxy.Fatal(ex, true);
            }
        }

        private static void LockClearThreadMethod()
        {
            while (true)
            {
                Thread.Sleep(LOCKECLEARINTERVAL);

                LockClear();
            }
        }

        private static void LockClear()
        {
            string[] lockIds = GetOverdueLock();
            if (lockIds == null || lockIds.Length == 0)
            {
                return;
            }

            LogProxy.InfoFormat(OVERDUELOCKSIZE, lockIds.Length);

            foreach (string lockId in lockIds)
            {
                Release(lockId);
            }
        }

        private static string[] GetOverdueLock()
        {
            lock (LockObj)
            {
                return (from o in LockExpireTime
                        where o.Value < DateTime.Now
                        select o.Key).ToArray();
            }
        }

        public bool GetLock(ILockItem[] items, out string lockId)
        {
            lockId = string.Empty;

            if (items == null || items.Length == 0)
            {
                return false;
            }

            int wait = 0;

        Next:
            bool isGetted = true;
            lock (LockObj)
            {
                foreach (ILockItem item in items)
                {
                    if (LockItems.ContainsKey(item))
                    {
                        isGetted = false;
                        break;
                    }
                }

                if (isGetted)
                {
                    lockId = Guid.NewGuid().ToString();
                    DateTime overdueTime = DateTime.Now.AddSeconds(LOCKEREXPIREINTERVAL);

                    MyLocks.Add(lockId, items);
                    LockExpireTime.Add(lockId, overdueTime);
                    foreach (ILockItem item in items)
                    {
                        LockItems.Add(item, lockId);
                    }

                    return true;
                }
            }

            if (!isGetted && wait < TRYCOUNT)
            {
                wait++;
                Thread.Sleep(TRYINTERVAL);
                goto Next;
            }

            return false;
        }

        private static void Release(string lockId)
        {
            if (string.IsNullOrEmpty(lockId))
            {
                return;
            }

            try
            {
                if (!MyLocks.ContainsKey(lockId))
                {
                    return;
                }

                lock (LockObj)
                {
                    if (!MyLocks.ContainsKey(lockId))
                    {
                        return;
                    }

                    ILockItem[] items = MyLocks[lockId];
                    foreach (ILockItem item in items)
                    {
                        LockItems.Remove(item);
                    }

                    MyLocks.Remove(lockId);
                    LockExpireTime.Remove(lockId);
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        public void ReleaseLock(string lockId)
        {
            Release(lockId);
        }

        public ILockItem CreateLockItem<T>(string id)
        {
            return new LockItem<T>(id);
        }

        private class LockItem<T> : LockItem
        {
            public override string TypeName
            {
                get
                {
                    return typeof(T).FullName;
                }
            }

            public LockItem(string id)
            {
                this.Id = id;
            }

            #region 操作符重载

            public static bool operator ==(LockItem<T> x, LockItem<T> y)
            {
                if (Object.ReferenceEquals(x, y))
                {
                    return true;
                }

                if (((object)x == null) || ((object)y == null))
                {
                    return false;
                }

                return string.Equals(x.TypeName, y.TypeName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
            }
            public static bool operator !=(LockItem<T> x, LockItem<T> y)
            {
                return !(x == y);
            }
            public override bool Equals(Object obj)
            {
                LockItem<T> p = obj as LockItem<T>;
                if ((object)p == null)
                {
                    return false;
                }

                return this == p;
            }
            public bool Equals(LockItem<T> p)
            {
                return this == p;
            }
            public override int GetHashCode()
            {
                string hCode = string.Format("{0}.{1}", TypeName, Id);
                return hCode.GetHashCode();
            }

            #endregion
        }

        private abstract class LockItem : ILockItem
        {
            public abstract string TypeName { get; }

            public string Id
            {
                get
                {
                    return _id;
                }
                set
                {
                    _id = value == null ? "" : value.ToLower().Trim();
                }
            }

            private string _id;
        }
    }
}
