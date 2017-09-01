using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Scree.Common;
using Scree.Core.IoC;
using Scree.Log;
using Scree.Persister;

namespace MyApp.Models
{
    public static class LogService
    {
        public static IPersisterService PersisterService
        {
            get
            {
                return ServiceRoot.GetService<IPersisterService>();
            }
        }

        internal static SystemLog CreateSystemLog(SystemLogType systemLogType, Type objectType, string objectId, string remark)
        {
            SystemLog systemLog = PersisterService.CreateObject<SystemLog>();

            systemLog.SystemLogType = systemLogType;
            systemLog.ObjectType = objectType.FullName;
            systemLog.ObjectId = objectId;
            systemLog.Remark = remark;

            return systemLog;
        }

        internal static void SROLogging(SRO[] objs)
        {
            try
            {
                List<SRO> list = new List<SRO>();
                SROLog log;
                foreach (SRO obj in objs)
                {
                    if (obj == null || obj.SaveMode == SROSaveMode.Init || obj.SaveMode == SROSaveMode.NoChange
                        || !(obj is INeedFullLogging))
                    {
                        continue;
                    }

                    log = new SROLog();
                    log.ObjectId = obj.Id;
                    log.ObjectType = obj.GetType().FullName;
                    log.ObjectJson = JsonConvert.SerializeObject(obj);
                    log.HostName = Tools.GetHostName();
                    list.Add(log);
                }

                if (list.Count > 0)
                {
                    PersisterService.SaveObject(list.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

    }
}