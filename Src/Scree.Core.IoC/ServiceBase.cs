using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Scree.Log;

namespace Scree.Core.IoC
{
    /// <summary>
    /// 所有服务的入口
    /// </summary>
    public abstract class ServiceBase : MarshalByRefObject, IService
    {
        public abstract bool Init();
        public virtual bool Run()
        {
            return true;
        }

        public bool IsInitialized { get; internal set; }
        public bool IsRunned { get; internal set; }
    }
}
