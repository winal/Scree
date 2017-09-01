using System;
using System.Collections.Generic;
using System.Text;

namespace Winal.Core
{
    /// <summary>
    /// 所有服务的基本入口
    /// </summary>
    public interface IServiceBase
    {
        /// <summary>
        /// 增加服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceInstance">服务实例</param>
        void AddService(Type serviceType, object serviceInstance);

        /// <summary>
        /// 移除服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        void RemoveService(Type serviceType);

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="throwException">是否抛出异常</param>
        /// <returns></returns>
        object GetService(Type serviceType, bool throwException);

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="serviceType">服务类型，不抛出异常</param>
        object GetService(Type serviceType);

        /// <summary>
        /// 服务是否存在
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns></returns>
        bool ExistService(Type serviceType);

        /// <summary>
        /// 替换服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="newInstance">新服务实例</param>
        /// <param name="isThrowOut">是否抛出异常</param>
        void ReplaceService(Type serviceType, object newInstance, bool isThrowOut);

        /// <summary>
        /// 替换服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="newInstance">新服务实例，不抛出异常</param>
        void ReplaceService(Type serviceType, object newInstance);

    }
}
