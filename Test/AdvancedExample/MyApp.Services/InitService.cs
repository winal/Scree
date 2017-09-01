using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Core.IoC;
using Scree.Persister;
using Scree.Log;
using Scree.DataBase;
using Scree.Common;
using System.Reflection;
using MyApp.Models;
using Newtonsoft.Json;
using System.Threading;
using System.Net;

namespace MyApp.Services
{
    public class InitService : ServiceBase
    {
        private const string SERVICEINITFAIL = "InitService init fail, {0} is null";

        public static IPersisterService PersisterService
        {
            get
            {
                return ServiceRoot.GetService<IPersisterService>();
            }
        }
        private static IMappingService MappingService
        {
            get
            {
                return ServiceRoot.GetService<IMappingService>();
            }
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Run()
        {
            PersisterService.RegisterAfterSaveMothed(SROLogging);

            return true;
        }

        private static void SROLogging(SRO[] objs)
        {
            if (objs == null)
            {
                return;
            }

            Thread t = new Thread(SROLoggingThreadMothed);
            t.Start(objs);
        }

        private static void SROLoggingThreadMothed(object o)
        {
            try
            {
                SRO[] objs = (SRO[])o;

                LogService.SROLogging(objs);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
    }
}
