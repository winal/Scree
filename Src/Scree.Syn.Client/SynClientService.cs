using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using Scree.Cache;
using Scree.Core.IoC;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;

namespace Scree.Syn.Client
{
    public sealed class SynClientService : ServiceBase, ISynClientService
    {
        private static ICacheService CacheService
        {
            get
            {
                return ServiceRoot.GetService<ICacheService>();
            }
        }

        private static ISynServerService SynServerService { get; set; }

        private static string ServerURI;//Remoting服务地址

        private static List<ISynData> PostData = new List<ISynData>();
        private static List<ISynData> GetData = new List<ISynData>();
        private static bool IsGetting;
        private static readonly object LockPostObj = new object();
        private static readonly object LockGetObj = new object();
        private static int PostDueTime = 0 * 1000;//发送待同步数据启动定时器延迟时间，单位ms, 默认0s
        private static int PostPeriod = 3 * 1000;//发送待同步数据定时器执行时间间隔，单位ms，默认3000ms

        private static int GetDueTime = 0 * 1000;//获取待同步数据启动定时器延迟时间，单位ms, 默认0s
        private static int GetPeriod = 3 * 1000;//获取待同步数据定时器执行时间间隔，单位ms，默认3000ms

        private static int PostTryInterval = 3 * 1000;//发送待同步数据失败重试间隔时间，单位ms，默认3000ms
        private static int PostTryMultiple = 3;//发送待同步数据失败重试间隔时间累积倍数，默认3
        private static int PostTrys = 4;//发送待同步数据失败重试次数，默认4

        private static int ClearInterval = 600 * 1000;//同步数据清理间隔，单位ms，默认600s

        private const string SERVICEINITFAIL = "SynClientService init fail, because {0} is null";
        private const string POSTFAIL = "Post syn data fail, it's size is {0}";
        private const string SERVERNOTE = "Current syn server appid is {0}, current syn index is {1}";

        private static long CurrentIndex = -1;
        private static string SynServerAppId;

        private static Timer PostTimer;
        private static Timer GetTimer;

        public override bool Init()
        {
            if (CacheService == null)
            {
                LogProxy.FatalFormat(SERVICEINITFAIL, "CacheService", true);
            }

            if (!CacheService.IsInitialized)
            {
                return false;
            }

            SynClientServiceInit();

            return true;
        }

        private static void SynClientServiceInit()
        {
            SynClientCommConfig.Init();
            SynClientConfig.Init();

            ServerURI = SynClientCommConfig.ServerURI;

            if (SynClientCommConfig.PostDueTime > 0)
            {
                PostDueTime = SynClientCommConfig.PostDueTime * 1000;
            }

            if (SynClientCommConfig.PostPeriod > 0)
            {
                PostPeriod = SynClientCommConfig.PostPeriod;
            }

            if (SynClientCommConfig.GetDueTime > 0)
            {
                GetDueTime = SynClientCommConfig.GetDueTime * 1000;
            }

            if (SynClientCommConfig.GetPeriod > 0)
            {
                GetPeriod = SynClientCommConfig.GetPeriod;
            }

            if (SynClientCommConfig.PostTryInterval > 0)
            {
                PostTryInterval = SynClientCommConfig.PostTryInterval;
            }

            if (SynClientCommConfig.PostTryMultiple > 0)
            {
                PostTryMultiple = SynClientCommConfig.PostTryMultiple;
            }

            if (SynClientCommConfig.PostTrys > 0)
            {
                PostTrys = SynClientCommConfig.PostTrys;
            }

            if (SynClientCommConfig.ClearInterval > 0)
            {
                ClearInterval = SynClientCommConfig.ClearInterval * 1000;
            }

            SynServerService = (ISynServerService)Activator.GetObject(typeof(ISynServerService), ServerURI);

            TimerCallback callback;

            callback = new TimerCallback(PostCallbackMethod);
            PostTimer = new Timer(callback, null, PostDueTime, PostPeriod);

            CurrentIndex = SynServerService.CurrentIndex;
            SynServerAppId = SynServerService.CurrentAppId;
            LogProxy.InfoFormat(SERVERNOTE, SynServerAppId, CurrentIndex);

            callback = new TimerCallback(GetCallbackMethod);
            GetTimer = new Timer(callback, null, GetDueTime, GetPeriod);

            Thread t = new Thread(SynClearThreadMethod);
            t.Start();
        }

        private static void SynClearThreadMethod()
        {
            while (true)
            {
                Thread.Sleep(ClearInterval);

                SynClear();
            }
        }

        private static void SynClear()
        {
            if (GetData.Count < 500)
            {
                return;
            }

            lock (LockGetObj)
            {
                int count = GetData.Count;
                ISynData data;
                for (int i = count - 1; i >= 0; i--)
                {
                    data = GetData[i];
                    if (!CacheService.IsCached(data.TypeName, data.Id))
                    {
                        GetData.RemoveAt(i);
                    }
                }
            }
        }

        private static void PostCallbackMethod(object obj)
        {
            if (PostData.Count == 0)
            {
                return;
            }

            lock (LockPostObj)
            {
                if (PostData.Count == 0)
                {
                    return;
                }

                Thread t = new Thread(PostCallbackThreadMethod);
                t.Start(PostData.ToArray());

                PostData.Clear();
            }
        }
        private static void PostCallbackThreadMethod(object obj)
        {
            ISynData[] objs = obj as ISynData[];

            if (objs == null || objs.Length == 0)
            {
                return;
            }

            int cnt = 0;
            int interval = PostTryInterval;
        L:
            try
            {
                SynServerService.Add(objs);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false, string.Format(POSTFAIL, objs.Length));
                if (cnt < PostTrys)
                {
                    Thread.Sleep(interval);
                    cnt++;
                    interval *= PostTryMultiple;
                    goto L;
                }
            }
        }

        private static void GetCallbackMethod(object obj)
        {
            if (IsGetting)
            {
                return;
            }

            lock (LockGetObj)
            {
                if (IsGetting)
                {
                    return;
                }

                IsGetting = true;
            }

            ISynData[] synData;
            try
            {
                SynGetted getted = SynServerService.Get(CurrentIndex);
                if (getted == null)
                {
                    return;
                }

                CurrentIndex = getted.Index;
                synData = getted.SynData;
                if (synData == null || synData.Length == 0)
                {
                    string appId = SynServerService.CurrentAppId;
                    if (!string.Equals(appId, SynServerAppId, StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentIndex = -1;
                        SynServerAppId = appId;
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
                return;
            }
            finally
            {
                IsGetting = false;
            }

            Getted(synData);
        }

        private static void Getted(ISynData[] synData)
        {
            try
            {
                lock (LockGetObj)
                {
                    foreach (var data in synData)
                    {
                        Getted(data);
                    }
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void Getted(ISynData data)
        {
            if (data == null || IsBan(data.TypeName) || !CacheService.IsCached(data.TypeName, data.Id))
            {
                return;
            }

            int index = GetData.IndexOf(data);
            if (index >= 0)
            {
                if (GetData[index].Version < data.Version)
                {
                    GetData[index] = data;
                }
            }
            else
            {
                GetData.Add(data);
            }
        }

        private static ISynData CreateSynData(string typeName, string id)
        {
            return SynData.Create(typeName, id.ToLower());
        }
        private static ISynData CreateSynData(string typeName, string id, long version)
        {
            return SynData.Create(typeName, id.ToLower(), version);
        }

        public void Add(ISynable synData)
        {
            if (synData == null)
            {
                return;
            }

            try
            {
                string typeName = synData.GetType().FullName;
                if (IsBan(typeName))
                {
                    return;
                }

                ISynData data = CreateSynData(typeName, synData.Id, synData.Version);

                lock (LockPostObj)
                {
                    int index = PostData.IndexOf(data);
                    if (index >= 0)
                    {
                        if (PostData[index].Version < data.Version)
                        {
                            PostData[index] = data;
                        }
                        return;
                    }
                    PostData.Add(data);
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        public bool IsLazy<T>()
        {
            SynClientConfig config = SynClientConfig.Get<T>();

            if (config == null)
            {
                return false;
            }
            return config.IsLazy;
        }

        private static bool IsBan(string typeName)
        {
            SynClientConfig config = SynClientConfig.Get(typeName);

            if (config == null)
            {
                return false;
            }
            return config.IsBan;
        }

        public bool Peek<T>(string id, out long version) where T : ISynable
        {
            version = 0;

            lock (LockGetObj)
            {
                int index = GetData.IndexOf(CreateSynData(typeof(T).FullName, id));
                if (index >= 0)
                {
                    version = GetData[index].Version;
                    GetData.RemoveAt(index);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
