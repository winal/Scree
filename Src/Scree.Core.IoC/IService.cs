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
    public interface IService
    {
        /// <summary>
        /// 预处理，启动内部服务，构筑运行环境（仅内部调用）
        /// </summary>
        bool Init();

        bool IsInitialized { get; }

        bool Run();
        bool IsRunned { get; }

    }
}
