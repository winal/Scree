using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Scree.Common;
using Scree.Core.IoC;
using Scree.Persister;

namespace MyUI.Models
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

    }
}