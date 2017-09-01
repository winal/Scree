using System;
using System.Collections.Generic;
using System.Text;

namespace Scree.Attributes
{
    /// <summary>
    /// string类型的属性映射为数据库字段的类型
    /// </summary>
    public enum StringType
    {
        /// <summary>
        /// nvarchar类型
        /// </summary>
        NVarchar = 0,

        /// <summary>
        /// ntext类型
        /// </summary>
        NText = 1
    }
}
