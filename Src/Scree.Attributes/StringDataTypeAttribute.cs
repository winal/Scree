using System;

namespace Scree.Attributes
{
    /// <summary>
    /// 基本数据类型（string）特性，指示该属性在创建对应数据字段时的设置
    /// </summary>    
    public class StringDataTypeAttribute : DataTypeAttribute
    {
        private int length = 32;
        private bool ismaxlength = false;
        private StringType type = StringType.NVarchar;

        /// <summary>
        /// 当枚举选择NVarchar时，数据库字段的长度
        /// </summary>
        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        /// <summary>
        /// 当枚举选择NVarchar时，数据库字段的长度为Max，若IsMaxLength=true且同时设置Length，则以IsMaxLength为准
        /// </summary>
        public bool IsMaxLength
        {
            get
            {
                return ismaxlength;
            }
            set
            {
                ismaxlength = value;
            }
        }

        /// <summary>
        /// string类型的属性映射为数据库字段的类型
        /// </summary>
        public StringType Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// 基本数据类型（string）特性，指示该属性在创建对应数据字段时的设置
        /// </summary>
        /// <param name="type">string类型的属性映射为数据库字段的类型</param>
        public StringDataTypeAttribute(StringType type)
        {
            this.type = type;
        }

        /// <summary>
        /// 基本数据类型（string）特性，指示该属性在创建对应数据字段时的设置
        /// </summary>
        public StringDataTypeAttribute()
        {

        }
    }
}
