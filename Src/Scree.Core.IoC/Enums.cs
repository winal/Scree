using System;
using System.Collections.Generic;
using System.Text;

namespace Scree.Core.IoC
{
    /// <summary>
    /// 应用程序启动状态
    /// </summary>
    public enum InitStatus
    {
        /// <summary>
        /// 未启动
        /// </summary>
        None = 0,

        /// <summary>
        /// 启动中
        /// </summary>
        Initing = 1,

        /// <summary>
        /// 启动完成
        /// </summary>
        Complete = 2,

        /// <summary>
        /// 启动失败
        /// </summary>
        Fail = 3
    }

}
