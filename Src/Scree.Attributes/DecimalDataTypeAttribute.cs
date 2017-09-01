using System;
using Scree.Log;

namespace Scree.Attributes
{
    /// <summary>
    /// 基本数据类型（decimal）特性，指示该属性在创建对应数据字段时的设置
    /// </summary>    
    public class DecimalDataTypeAttribute : DataTypeAttribute
    {
        private const string PRECISIONERROR = "Precision must be between 1 and 38";
        private const string DECIMALDIGITSERROR = "Decimal Digits must be less than or equal to precision between 0 and 38";

        private int precision = 18;
        private int decimaldigits = 4;

        /// <summary>
        /// 精度
        /// </summary>
        public int Precision
        {
            get
            {
                return precision;
            }
        }

        /// <summary>
        /// 小数位数
        /// </summary>
        public int DecimalDigits
        {
            get
            {
                return decimaldigits;
            }
        }

        /// <summary>
        /// 基本数据类型（decimal）特性，指示该属性在创建对应数据字段时的设置
        /// </summary>
        /// <param name="precision">精度（1-38之间）</param>
        /// <param name="decimalDigits">小数位数（0-38之间）且必须小于或等于精度</param>
        public DecimalDataTypeAttribute(int precision, int decimalDigits)
        {
            if (precision < 1 || precision > 38)
            {
                LogProxy.Fatal(PRECISIONERROR, true);
            }
            if (decimalDigits < 0 || decimalDigits > precision)
            {
                LogProxy.Fatal(DECIMALDIGITSERROR, true);
            }

            this.precision = precision;
            this.decimaldigits = decimalDigits;
        }

        /// <summary>
        /// 基本数据类型（decimal）特性，指示该属性在创建对应数据字段时的设置
        /// </summary>
        public DecimalDataTypeAttribute()
        {

        }
    }
}
