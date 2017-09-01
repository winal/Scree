using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using Scree.Core.IoC;

namespace Scree.Syn.Server
{
    public class SynServerService : ServiceBase, ISynServerService
    {
        private static readonly string AppId = Guid.NewGuid().ToString();

        private static DateTime LastClearTime = DateTime.Now;
        private static readonly string ApplicationName = "SynServerService";//Remoting ApplicationName
        private static readonly int Port = 8088;//Remoting 端口
        private static readonly int ClearInterval = 5 * 1000;//过期同步数据清理时间间隔，单位ms，默认300s
        private static readonly int TimeLimit = 15;//待同步数据保留时间，单位M，默认15M

        private const string CURRENTAPPID = "Current appid is: {0}";
        private const string BEGINCLEAR = "Begin syn server clear, length: {0}";
        private const string ENDCLEAR = "End syn server clear, length: {0}";

        public override bool Init()
        {
            TcpChannel channel = new TcpChannel(Port);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(SynServerService), ApplicationName, WellKnownObjectMode.Singleton);

            Thread t = new Thread(ClearThreadMethod);
            t.Start();

            return true;
        }

        static SynServerService()
        {
            try
            {
                LogProxy.InfoFormat(CURRENTAPPID, AppId);

                ApplicationName = SynServerConfig.ApplicationName;

                if (SynServerConfig.Port > 0)
                {
                    Port = SynServerConfig.Port;
                }

                if (SynServerConfig.ClearInterval > 0)
                {
                    ClearInterval = SynServerConfig.ClearInterval * 1000;
                }

                if (SynServerConfig.TimeLimit > 0)
                {
                    TimeLimit = SynServerConfig.TimeLimit;
                }
            }
            catch (Exception ex)
            {
                LogProxy.Fatal(ex, true);
            }
        }

        private static void ClearThreadMethod()
        {
            while (true)
            {
                Thread.Sleep(ClearInterval);

                Clear();
            }
        }

        private static void Clear()
        {
            LogProxy.InfoFormat(BEGINCLEAR, Data.Count);

            DateTime clearTime = DateTime.Now.AddMinutes(-TimeLimit);

            if (LockDicObj.TryEnterWriteLock(WriteLockTimeout))
            {
                try
                {
                    var keys = (from o in Data
                                where o.Value.SynTime < clearTime
                                select o.Key).ToArray();

                    foreach (var key in keys)
                    {
                        Data.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
                finally
                {
                    LockDicObj.ExitWriteLock();
                }
            }

            LogProxy.InfoFormat(ENDCLEAR, Data.Count);
        }

        private static object LockObj = new object();
        private static readonly int WriteLockTimeout = 5 * 1000;//获取更新锁超时时间，单位毫秒，默认5000ms
        private static readonly ReaderWriterLockSlim LockDicObj = new ReaderWriterLockSlim();
        private static long Index = -1;
        private static Dictionary<long, MySynData> Data = new Dictionary<long, MySynData>(62851);

        public long CurrentIndex
        {
            get
            {
                return Index;
            }
        }

        public string CurrentAppId
        {
            get
            {
                return AppId;
            }
        }

        public void Add(ISynData[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            if (LockDicObj.TryEnterWriteLock(WriteLockTimeout))
            {
                try
                {
                    foreach (ISynData d in data)
                    {
                        if (d == null)
                        {
                            continue;
                        }
                        Index++;
                        Data.Add(Index, new MySynData(d, DateTime.Now));
                    }
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, true);
                }
                finally
                {
                    LockDicObj.ExitWriteLock();
                }
            }
        }

        public SynGetted Get(long lastIndex)
        {
            SynGetted getted = new SynGetted();
            getted.Index = lastIndex;

            LockDicObj.EnterReadLock();

            try
            {
                if (Data.Count != 0)
                {
                    getted.Index = Data.Keys.Max();
                    getted.SynData = (from d in Data where d.Key > lastIndex select d.Value.SynData).ToArray();
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
            finally
            {
                LockDicObj.ExitReadLock();
            }

            return getted;
        }

        private class MySynData
        {
            public ISynData SynData { get; set; }
            public DateTime SynTime { get; set; }

            public MySynData(ISynData synData, DateTime synTime)
            {
                this.SynData = synData;
                this.SynTime = synTime;
            }
        }
    }
}
