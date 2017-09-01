using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Xml;
using System.Xml.Linq;
using Scree.Log;
using System.Reflection;
using Scree.Common;

namespace Scree.DataBase
{
    public sealed class DbParameterProxy : IMyDbParameter
    {
        private const string TOSTRING = "{{ParameterName:{0};DbType:{1};Direction:{2};Size:{3};Value:{4};}}";
        public SqlParameter Para { get; private set; }

        public string ParameterName
        {
            get
            {
                return Para.ParameterName;
            }
            set
            {
                Para.ParameterName = value;
            }
        }
        public int Size
        {
            get
            {
                return Para.Size;
            }
            set
            {
                Para.Size = value;
            }
        }
        public object Value
        {
            get
            {
                return Para.Value;
            }
            set
            {
                Para.Value = value;
            }
        }
        public SqlDbType DbType
        {
            get
            {
                return Para.SqlDbType;
            }
            set
            {
                Para.SqlDbType = value;
            }
        }
        public ParameterDirection Direction
        {
            get
            {
                return Para.Direction;
            }
            set
            {
                Para.Direction = value;
            }
        }

        private DbParameterProxy()
        {
        }


        public static IMyDbParameter Create(string parameterName, SqlDbType dbType, object value)
        {
            DbParameterProxy proxy = new DbParameterProxy();

            SqlParameter para = new SqlParameter(parameterName, dbType);
            if (value == null)
            {
                para.Value = DBNull.Value;
            }
            else
            {
                para.Value = value;
            }

            proxy.Para = para;

            return proxy;
        }
        public static IMyDbParameter Create(string parameterName, SqlDbType dbType, int size, object value)
        {
            DbParameterProxy proxy = new DbParameterProxy();

            SqlParameter para = new SqlParameter(parameterName, dbType, size);
            if (value == null)
            {
                para.Value = DBNull.Value;
            }
            else
            {
                para.Value = value;
            }

            proxy.Para = para;

            return proxy;
        }
        public static IMyDbParameter Create(string parameterName, SqlDbType dbType, ParameterDirection direction)
        {
            DbParameterProxy proxy = new DbParameterProxy();

            SqlParameter para = new SqlParameter(parameterName, dbType);
            para.Direction = direction;

            proxy.Para = para;

            return proxy;
        }

        public override string ToString()
        {
            return string.Format(TOSTRING, ParameterName, DbType, Direction, Size, Value);
        }

    }
}
