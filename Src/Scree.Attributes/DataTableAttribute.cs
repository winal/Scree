using System;

namespace Scree.Attributes
{
    /// <summary>
    /// 基本表特性，指示该类或继承了该接口的类是否需要在数据库中创建对应的存储表
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class DataTableAttribute : Attribute
    {

    }
}
