using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Log;
using System.Xml.Linq;
using System.Reflection;
using Scree.Common;

namespace Scree.Core.IoC
{
    public static class ServiceRoot
    {
        public static InitStatus InitStatus { get; private set; }
        private static object LockObj = new object();
        private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + @"config\root.config";
        private const string SERVICELOADED = "Service load succeed : {0}";
        private const string SERVICELOADFAIL = "Service load fail : {0}";
        private const string GETSERVICETYPENULL = "Get service error, {0} type is null";

        private const string SERVICESBEGININIT = "Services begin init";
        private const string SERVICESENDINIT = "Services end init";

        private const string SERVICESINITERROR = "Services init error";
        private const string SERVICEBEGININIT = "Service {0} begin init";
        private const string SERVICEINITFAIL = "Service {0} init fail";
        private const string SERVICEENDINIT = "Service {0} end init";

        private const string SERVICEBEGINRUN = "Service {0} begin run";
        private const string SERVICERUNFAIL = "Service {0} run fail";
        private const string SERVICERUNFAILNOINIT = "Service {0} run fail, no init";
        private const string SERVICEENDRUN = "Service {0} end run";

        private static Dictionary<Type, IService> Services = new Dictionary<Type, IService>(29);

        /// <summary>
        /// 预处理，启动内部服务，构筑运行环境，仅调用一次
        /// </summary>
        public static void Init()
        {
            if (InitStatus != InitStatus.None)
            {
                return;
            }

            lock (LockObj)
            {
                if (InitStatus != InitStatus.None)
                {
                    return;
                }

                InitStatus = InitStatus.Initing;
            }

            LogProxy.Info(SERVICESBEGININIT);

            try
            {
                ServicesLoad();

                ServicesInit();

                ServicesRun();
            }
            catch (Exception ex)
            {
                InitStatus = InitStatus.Fail;
                LogProxy.Fatal(ex, true);
            }

            InitStatus = InitStatus.Complete;

            LogProxy.Info(SERVICESENDINIT);
        }

        public static T GetService<T>() where T : IService
        {
            Type type = typeof(T);

            if (!Services.ContainsKey(type))
            {
                LogProxy.InfoFormat(GETSERVICETYPENULL, type);
                return default(T);
            }

            IService obj = Services[type];
            if (!obj.IsInitialized)
            {
                return default(T);
            }

            return (T)obj;
        }


        private static void AddService(Type type, IService obj)
        {
            Services[type] = obj;
        }

        private static void ServicesInit()
        {
            int fails = 0;
            while (true)
            {
                int lastFails = fails;
                fails = 0;

                bool isInitialized = true;
                foreach (var v in Services.Values)
                {
                    if (v.IsInitialized)
                    {
                        continue;
                    }

                    LogProxy.Info(string.Format(SERVICEBEGININIT, v.ToString()));

                    ((ServiceBase)v).IsInitialized = v.Init();

                    if (v.IsInitialized)
                    {
                        LogProxy.Info(string.Format(SERVICEENDINIT, v.ToString()));
                    }
                    else
                    {
                        LogProxy.Info(string.Format(SERVICEINITFAIL, v.ToString()));
                        fails++;
                        isInitialized = false;
                    }
                }

                if (isInitialized)
                {
                    return;
                }

                if (lastFails <= fails)
                {
                    LogProxy.Fatal(SERVICESINITERROR, true);
                }
            }
        }

        private static void ServicesRun()
        {
            foreach (var v in Services.Values)
            {
                if (!v.IsInitialized)
                {
                    LogProxy.Fatal(string.Format(SERVICERUNFAILNOINIT, v.ToString()), true);
                    continue;
                }

                LogProxy.Info(string.Format(SERVICEBEGINRUN, v.ToString()));

                ((ServiceBase)v).IsRunned = v.Run();

                if (v.IsRunned)
                {
                    LogProxy.Info(string.Format(SERVICEENDRUN, v.ToString()));
                }
                else
                {
                    LogProxy.Fatal(string.Format(SERVICERUNFAIL, v.ToString()), true);
                }
            }
        }

        private static void ServicesLoad()
        {
            XElement root = XElement.Load(ConfigDirectory);
            IEnumerable<XElement> services = root.Elements("Service");

            string dllDir = Tools.GetAssemblyPath();

            Assembly driverAssembly, typeAssembly;
            Type typeType, driverType;
            IService obj;
            foreach (var service in services)
            {
                string[] type = service.Attribute("type").Value.Split(',');
                string[] driver = service.Attribute("driver").Value.Split(',');

                driverAssembly = Assembly.LoadFrom(string.Format("{0}{1}.dll", dllDir, driver[1].Trim(), ".dll"));
                typeAssembly = Assembly.LoadFrom(string.Format("{0}{1}.dll", dllDir, type[1].Trim(), ".dll"));

                typeType = typeAssembly.GetType(type[0].Trim());
                driverType = driverAssembly.GetType(driver[0].Trim());

                if (typeType == null || driverType == null)
                {
                    LogProxy.Fatal(string.Format(SERVICELOADFAIL, driver[0].Trim()), true);
                }

                obj = Activator.CreateInstance(driverType) as IService;

                if (obj == null)
                {
                    LogProxy.Fatal(string.Format(SERVICELOADFAIL, driver[0].Trim()), true);
                }

                LogProxy.Info(string.Format(SERVICELOADED, driver[0].Trim()));

                AddService(typeType, obj);
            }
        }

    }
}
