using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Core.IoC;
using Scree.Syn.Client;

namespace Scree.Lock.Client
{
    public class LockService : ServiceBase, ILockService
    {
        private static readonly string ServerURI;//Remoting服务地址
        private static ILockService LockServerService { get; set; }

        public override bool Init()
        {
            return true;
        }

        static LockService()
        {
            ServerURI = LockClientConfig.ServerURI;

            LockServerService = (ILockService)Activator.GetObject(typeof(ILockService), ServerURI);
        }

        public bool GetLock(ILockItem[] items, out string lockId)
        {
            return LockServerService.GetLock(items, out lockId);
        }

        public void ReleaseLock(string lockId)
        {
            LockServerService.ReleaseLock(lockId);
        }

        public ILockItem CreateLockItem<T>(string id)
        {
            return LockServerService.CreateLockItem<T>(id);
        }
    }
}
