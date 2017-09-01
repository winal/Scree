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
    public sealed class DbProxy : IDbOperate
    {
        private const string TOSTRING = "Alias:{0}; Type:{1}; Assembly:{2}; StorageContext:{3}";
        private const string STORAGECONTEXTUNABLED = "StorageContext:{0} unabled";
        private const string GETSTORAGECONTEXTNULL = "Get StorageContext is null. Type:{0}; Alias:{1}";
        private string Alias { get; set; }
        private Type ObjType { get; set; }
        private StorageContext Context { get; set; }
        private SQLServer DbServer { get; set; }

        private DbProxy()
        {
        }
        private DbProxy(string alias, Type type)
        {
            this.Alias = alias;
            this.ObjType = type;
        }

        public static IDbOperate Create(string alias, Type type)
        {
            StorageContext storageContext = MappingService.GetStorageContext(alias, type);

            if (storageContext == null)
            {
                LogProxy.ErrorFormat(true, GETSTORAGECONTEXTNULL, type.FullName, alias);
                return null;
            }

            if (storageContext.Enabled == false)
            {
                LogProxy.ErrorFormat(true, STORAGECONTEXTUNABLED, storageContext.Name);
                return null;
            }

            DbProxy proxy = new DbProxy(alias, type);
            proxy.Context = storageContext;
            proxy.DbServer = new SQLServer(storageContext);
            proxy.DbServer.Alias = proxy.Alias;
            proxy.DbServer.ObjType = proxy.ObjType;
            return proxy;
        }

        #region IDBOperate 成员，非存储过程 开始

        public int ExecuteNonQuery(string sql, IMyDbParameter[] prams)
        {
            return DbServer.ExecuteNonQuery(sql, prams);
        }

        public int ExecuteNonQuery(string sql)
        {
            return DbServer.ExecuteNonQuery(sql);
        }

        public object ExecuteScalar(string sql, IMyDbParameter[] prams)
        {
            return DbServer.ExecuteScalar(sql, prams);
        }

        public object ExecuteScalar(string sql)
        {
            return DbServer.ExecuteScalar(sql);
        }

        public bool IsExist(string sql, IMyDbParameter[] prams)
        {
            return DbServer.IsExist(sql, prams);
        }

        public bool IsExist(string sql)
        {
            return DbServer.IsExist(sql);
        }

        public IDataReader GetDataReader(string sql, IMyDbParameter[] prams)
        {
            return DbServer.GetDataReader(sql, prams);
        }

        public IDataReader GetDataReader(string sql)
        {
            return DbServer.GetDataReader(sql);
        }

        public DataSet GetDataSet(string sql, IMyDbParameter[] prams)
        {
            return DbServer.GetDataSet(sql, prams);
        }

        public DataSet GetDataSet(string sql)
        {
            return DbServer.GetDataSet(sql);
        }

        #endregion IDBOperate 成员，非存储过程 结束


        #region IDBOperate 成员，存储过程 开始

        public void RunProcedure(string procName, IMyDbParameter[] prams, out Dictionary<string, object> returnValue)
        {
            DbServer.RunProcedure(procName, prams, out returnValue);
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet, out Dictionary<string, object> returnValue)
        {
            DbServer.RunProcedure(procName, prams, out dataSet, out returnValue);
        }

        //public void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader, out Dictionary<string, object> returnValue)
        //{
        //    DbServer.RunProcedure(procName, prams, out dataReader, out returnValue);
        //}

        public int RunProcedure(string procName, IMyDbParameter[] prams)
        {
            return DbServer.RunProcedure(procName, prams);
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet)
        {
            DbServer.RunProcedure(procName, prams, out dataSet);
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader)
        {
            DbServer.RunProcedure(procName, prams, out dataReader);
        }

        public int RunProcedure(string procName)
        {
            return DbServer.RunProcedure(procName);
        }

        public void RunProcedure(string procName, out DataSet dataSet)
        {
            DbServer.RunProcedure(procName, out dataSet);
        }

        public void RunProcedure(string procName, out IDataReader dataReader)
        {
            DbServer.RunProcedure(procName, out dataReader);
        }

        #endregion IDBOperate 成员，存储过程 结束

        public override string ToString()
        {
            return string.Format(TOSTRING, Alias, ObjType.FullName, ObjType.Assembly.FullName, Context.Name);
        }

    }
}
