using System;

namespace Scree.Attributes
{
    /// <summary>
    /// 基本数据类型特性，指示该属性在创建对应数据字段时的设置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DataTypeAttribute : Attribute
    {
        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable
        {
            get
            {
                return _isNullable;
            }
            set
            {
                _isNullable = value;
            }
        }
        private bool _isNullable = true;

        /// <summary>
        /// 是否需要加载值
        /// </summary>
        public bool IsLoad
        {
            get
            {
                return _isLoad;
            }
            set
            {
                _isLoad = value;
            }
        }
        private bool _isLoad = true;

        /// <summary>
        /// 是否需要保存
        /// </summary>
        public bool IsSave
        {
            get
            {
                return _isSave;
            }
            set
            {
                _isSave = value;
            }
        }
        private bool _isSave = true;
    }
}
