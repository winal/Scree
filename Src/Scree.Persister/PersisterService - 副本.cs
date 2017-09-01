using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;
using System.Collections;
using System.Reflection;
using Scree.DataBase;
using Scree.Log;
using Scree.Attributes;
using System.Transactions;
using Scree.Common;
using Scree.Cache;

namespace Scree.Persister
{
    public enum LoadType
    {
        CacheFirst = 0,
        OnlyCache = 1,
        DataBaseDirect = 2,
    }

    /// <summary>
    /// 对象持久化操作
    /// </summary>
    public sealed class PersisterService
    {
        private const string OBJECTISNULL = "Object for save is null";
        private const string OBJECTSISNULL = "Objects for save is null or it's size is zero";
        private const string OBJECTSISNOTMODIFIED = "Object is not modified. Type:{0}, Id:{1}";
        private const string IDISNULL = "Id is null when get single object";
        private const string DATATYPEERROR = "{0} data type is unallowed";
        private const string SELECTSINGLEOBJECT = "SELECT * FROM [{0}] WHERE Id = @Id";
        private const string SELECTSINGLEOBJECTWITHNOLOCK = "SELECT * FROM [{0}] WITH(NOLOCK) WHERE Id = @Id";
        private const string SELECTOBJECTS = "SELECT {0} * FROM [{1}] WHERE {2}";
        private const string SELECTOBJECTSWITHNOLOCK = "SELECT {0} * FROM [{1}] WITH(NOLOCK) WHERE {2}";

        public static object CreateObject(Type type)
        {
            object obj = Activator.CreateInstance(type);

            return obj;
        }
        public static T CreateObject<T>() where T : class, new()
        {
            T obj = (T)Activator.CreateInstance(typeof(T));

            return obj;
        }


        #region 保存对象 开始

        public static void SaveObject(SRO obj)
        {
            if (obj == null)
            {
                LogProxy.Warn(OBJECTISNULL);
                return;
            }

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    SaveSingleObject(obj);

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                obj.Reset();

                LogProxy.Error(ex, true);
            }

            obj.AfterSave();
        }

        public static void SaveObject(SRO[] objs)
        {
            if (objs == null || objs.Length == 0)
            {
                LogProxy.Warn(OBJECTSISNULL);
                return;
            }

            var objDistincted = objs.Distinct(new SROComparer());
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    foreach (SRO obj in objDistincted)
                    {
                        if (obj == null)
                        {
                            LogProxy.Warn(OBJECTISNULL);
                            continue;
                        }

                        SaveSingleObject(obj);
                    }

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                foreach (SRO obj in objDistincted)
                {
                    if (obj != null)
                    {
                        obj.Reset();
                    }
                }

                LogProxy.Error(ex, true);
            }

            foreach (SRO obj in objDistincted)
            {
                if (obj != null)
                {
                    obj.AfterSave();
                }
            }
        }

        #endregion 保存对象 开始


        #region 获得一组对象 开始

        public static T[] LoadObjects<T>(string alias, string tableName, int top, string condition, IMyDbParameter[] prams, bool isNolock) where T : SRO, new()
        {
            Type type = typeof(T);

            IDbOperate dbOperate = DbProxy.Create(alias, type);
            IDataReader dataReader = null;
            try
            {
                string sql = string.Format(isNolock ? SELECTOBJECTSWITHNOLOCK : SELECTOBJECTS,
                    (top <= 0 ? "" : "TOP " + top.ToString()),
                    tableName, condition);

                dataReader = dbOperate.GetDataReader(sql, prams);

                List<T> list = new List<T>();
                while (dataReader.Read())
                {
                    list.Add(LoadObject<T>(dataReader));
                }

                return list.ToArray();
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, true);
                throw;
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                }
            }
        }
        public static T[] LoadObjects<T>(string alias, int top, string condition, IMyDbParameter[] prams, bool isNolock) where T : SRO, new()
        {
            return LoadObjects<T>(alias, typeof(T).Name, top, condition, prams, false);
        }
        public static T[] LoadObjects<T>(string alias, string condition, IMyDbParameter[] prams) where T : SRO, new()
        {
            return LoadObjects<T>(alias, 0, condition, prams, false);
        }
        public static T[] LoadObjects<T>(string condition, IMyDbParameter[] prams) where T : SRO, new()
        {
            return LoadObjects<T>(null, 0, condition, prams, false);
        }

        #endregion 获得一组对象 结束


        #region 获得单个对象 开始
        
        public static SRO LoadObject(Type type, string alias, string tableName, string id, LoadType loadType, bool isNolock)
        {
            if (string.IsNullOrEmpty(id))
            {
                LogProxy.Error(IDISNULL, true);
                return null;
            }

            if (loadType != LoadType.DataBaseDirect)
            {
                SRO obj = LoadCacheObject(type, id) as SRO;

                if (loadType == LoadType.OnlyCache)
                {
                    return obj;
                }

                if (obj != null)
                {
                    return obj;
                }
            }

            IDbOperate dbOperate = DbProxy.Create(alias, type);
            IDataReader dataReader = null;
            try
            {
                string sql = string.Format(isNolock ? SELECTSINGLEOBJECTWITHNOLOCK : SELECTSINGLEOBJECT, tableName);

                IMyDbParameter[] prams = 
                {
                    DbParameterProxy.Create("@Id", SqlDbType.NVarChar, 32, id)
                };

                dataReader = dbOperate.GetDataReader(sql, prams);

                if (!dataReader.Read())
                {
                    return null;
                }

                SRO obj = LoadObject(type, dataReader);
                if (obj == null)
                {
                    return null;
                }
                obj.CurrentAlias = alias;
                obj.CurrentTableName = tableName;
                return obj;
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, true);
                throw;
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                }
            }
        }
        public static T LoadObject<T>(string alias, string tableName, string id, LoadType loadType, bool isNolock) where T : SRO, new()
        {
            return LoadObject(typeof(T), alias, tableName, id, loadType, isNolock) as T;
        }
        public static T LoadObject<T>(string alias, string id, LoadType loadType) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, loadType, false);
        }
        public static T LoadObject<T>(string alias, string id, bool isNolock) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, LoadType.CacheFirst, isNolock);
        }
        public static T LoadObject<T>(string alias, string id) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, LoadType.CacheFirst, false);
        }
        public static T LoadObject<T>(string id) where T : SRO, new()
        {
            return LoadObject<T>(null, typeof(T).Name, id, LoadType.CacheFirst, false);
        }

        #endregion 获得单个对象 结束


        #region private 开始

        private class SROComparer : EqualityComparer<SRO>
        {
            public override bool Equals(SRO o1, SRO o2)
            {
                if (o1.GetType() == o2.GetType() && string.Equals(o1.Id, o2.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode(SRO o)
            {
                string hCode = string.Format("{0}^{1}", o.GetType().ToString(), o.Id);
                return hCode.GetHashCode();
            }
        }

        private static void Insert(SRO obj)
        {
            Type type = obj.GetType();

            IMyDbParameter[] prams;
            string insertSql = SqlBuilder.InsertBuilder(obj, out prams);

            Save(obj, insertSql, prams);
        }

        private static void Update(SRO obj)
        {
            Type type = obj.GetType();

            IMyDbParameter[] prams;
            string updateSql = SqlBuilder.UpdateBuilder(obj, out prams);

            if (string.IsNullOrEmpty(updateSql))
            {
                LogProxy.InfoFormat(OBJECTSISNOTMODIFIED, obj.GetType(), obj.Id);
                return;
            }

            Save(obj, updateSql, prams);
        }

        private static void Save(SRO obj, string sql, IMyDbParameter[] prams)
        {
            string tableName;
            Type type = obj.GetType();

            string[] aliases = obj.Aliases;
            IDbOperate dbOperate;
            if (aliases.Length == 0)
            {
                tableName = GetTableName(obj, null);

                dbOperate = DbProxy.Create(null, type);
                dbOperate.ExecuteNonQuery(string.Format(sql, tableName), prams);
            }
            else
            {
                foreach (string alias in aliases)
                {
                    tableName = GetTableName(obj, alias);

                    dbOperate = DbProxy.Create(alias, type);
                    dbOperate.ExecuteNonQuery(string.Format(sql, tableName), prams);
                }
            }
        }

        private static string GetTableName(SRO obj, string alias)
        {
            return obj.GetTableName(alias);
        }

        private class SqlBuilder
        {
            private static Type INTTYPE = typeof(int);
            private static Type BOOLTYPE = typeof(bool);
            private static Type STRINGTYPE = typeof(string);
            private static Type DATETIMETYPE = typeof(DateTime);
            private static Type DECIMALTYPE = typeof(decimal);
            private static Type LONGTYPE = typeof(long);
            private static Type BYTESTYPE = typeof(byte[]);

            private static Dictionary<string, string> InsertSqls = new Dictionary<string, string>(89);

            public static string InsertBuilder(SRO obj, out IMyDbParameter[] prams)
            {
                prams = InsertParametersBuilder(obj);
                return GetInsertSql(obj.GetType());
            }

            private static string GetInsertSql(Type type)
            {
                string key = string.Format("{0};{1}", type.FullName, type.Assembly.FullName);

                if (InsertSqls.ContainsKey(key))
                {
                    return InsertSqls[key];
                }

                lock (InsertSqls)
                {
                    if (InsertSqls.ContainsKey(key))
                    {
                        return InsertSqls[key];
                    }

                    string insertSqls = InsertSqlBuilder(type);
                    InsertSqls[key] = insertSqls;

                    return insertSqls;
                }
            }

            private static IMyDbParameter ParameterBuilder(IMyPropertyInfo property, object pValue)
            {
                #region 构造 IMyDbParameter 开始
                Type pType = property.PropertyType;
                if (pType == INTTYPE)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.Int, pValue);
                }
                else if (pType == BOOLTYPE)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.Bit, pValue);
                }
                else if (pType == STRINGTYPE)
                {
                    if (property.StringDataType.Type == StringType.NVarchar)
                    {
                        if (property.StringDataType.IsMaxLength)
                        {
                            return DbParameterProxy.Create(property.Name, SqlDbType.NVarChar, pValue);
                        }
                        else
                        {
                            return DbParameterProxy.Create(property.Name, SqlDbType.NVarChar, property.StringDataType.Length, pValue);
                        }
                    }
                    else
                    {
                        return DbParameterProxy.Create(property.Name, SqlDbType.NText, pValue);
                    }
                }
                else if (pType == DATETIMETYPE)
                {
                    if ((DateTime)pValue == DateTime.MinValue)
                    {
                        return DbParameterProxy.Create(property.Name, SqlDbType.DateTime, DBNull.Value);
                    }
                    else
                    {
                        return DbParameterProxy.Create(property.Name, SqlDbType.DateTime, pValue);
                    }
                }
                else if (pType == DECIMALTYPE)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.Decimal, pValue);
                }
                else if (pType.IsEnum)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.Int, (int)pValue);
                }
                else if (pType == LONGTYPE)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.BigInt, pValue);
                }
                else if (pType == BYTESTYPE)
                {
                    return DbParameterProxy.Create(property.Name, SqlDbType.Image, pValue);
                }
                else
                {
                    LogProxy.ErrorFormat(true, DATATYPEERROR, pType.ToString());
                    return null;
                }
                #endregion 构造 IMyDbParameter 结束
            }

            private static IMyDbParameter[] InsertParametersBuilder(SRO obj)
            {
                List<IMyDbParameter> prams = new List<IMyDbParameter>();

                IMyPropertyInfo[] myPropertys = PropertyInfoProxy.GetProperties(obj.GetType());

                foreach (IMyPropertyInfo property in myPropertys)
                {
                    if (property.IsSave == false)
                    {//判断是否保存
                        continue;
                    }

                    object pValue = property.GetValue(obj);
                    if (pValue == null)
                    {
                        pValue = DBNull.Value;
                    }

                    prams.Add(ParameterBuilder(property, pValue));
                }

                return prams.ToArray();
            }

            private static string InsertSqlBuilder(Type type)
            {
                StringBuilder sql = new StringBuilder("INSERT INTO [{0}] (");
                StringBuilder values = new StringBuilder();

                IMyPropertyInfo[] myPropertys = PropertyInfoProxy.GetProperties(type);
                foreach (IMyPropertyInfo property in myPropertys)
                {
                    if (property.IsSave == false)
                    {//判断是否保存
                        continue;
                    }

                    sql.Append("[");
                    sql.Append(property.Name);
                    sql.Append("],");

                    values.Append("@");
                    values.Append(property.Name);
                    values.Append(",");
                }

                sql.Remove(sql.Length - 1, 1);
                values.Remove(values.Length - 1, 1);

                sql.Append(") VALUES (");
                sql.Append(values);
                sql.Append(") ");

                return sql.ToString();
            }

            public static string UpdateBuilder(SRO obj, out IMyDbParameter[] prams)
            {
                List<IMyDbParameter> pramList = new List<IMyDbParameter>();
                Type type = obj.GetType();

                StringBuilder sql = new StringBuilder("UPDATE [{0}] SET ");

                bool isModified = false;
                IMyPropertyInfo[] myPropertys = PropertyInfoProxy.GetProperties(obj.GetType());
                foreach (IMyPropertyInfo property in myPropertys)
                {
                    if (property.IsSave == false || property.Name == "CreatedDate" || property.Name == "LastAlterDate")
                    {//判断是否保存
                        continue;
                    }

                    object pValue = property.GetValue(obj);
                    if (property.Name == "Id")
                    {
                        pramList.Add(ParameterBuilder(property, pValue));
                        continue;
                    }

                    if (object.Equals(obj.GetOriginalValue(property.Name), pValue))
                    {
                        continue;
                    }

                    isModified = true;

                    sql.Append("[");
                    sql.Append(property.Name);
                    sql.Append("]=@");
                    sql.Append(property.Name);
                    sql.Append(",");

                    pramList.Add(ParameterBuilder(property, pValue));
                }

                if (isModified == false)
                {
                    prams = null;
                    return null;
                }

                prams = pramList.ToArray();
                sql.Append(string.Format("[LastAlterDate]='{0}' WHERE [Id]=@Id", obj.LastAlterDate));

                return sql.ToString();
            }
        }

        private static T LoadObject<T>(IDataReader dataReader) where T : SRO, new()
        {
            return LoadObject(typeof(T), dataReader) as T;
        }

        private static SRO LoadObject(Type type, IDataReader dataReader)
        {
            SRO obj = CreateObject(type) as SRO;
            if (obj == null)
            {
                return null;
            }

            IMyPropertyInfo[] myPropertys = PropertyInfoProxy.GetProperties(type);
            foreach (IMyPropertyInfo property in myPropertys)
            {
                if (property.IsLoad == false)
                {//判断是否加载
                    continue;
                }

                object value = dataReader[property.Name];

                if (value == DBNull.Value)
                {
                    continue;
                }

                property.SetValue(obj, value);
                obj.SetOriginalValue(property.Name, property.GetValue(obj));
            }

            IMyPropertyInfo isNew = PropertyInfoProxy.GetProperty(type, "IsNew");
            isNew.SetValue(obj, false);


            AddCache(obj);

            return obj;
        }

        private static void AddCache(ICacheable obj)
        {
            try
            {
                CacheService.Set(obj);
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }
        private static ICacheable LoadCacheObject(Type type, string id)
        {
            return CacheService.Get(type, id);
        }
        private static T LoadCacheObject<T>(string id) where T : SRO
        {
            return CacheService.Get(typeof(T), id) as T;
        }

        private static void SaveSingleObject(SRO obj)
        {
            obj.LastAlterDate = DateTime.Now;
            obj.BeforeSave();

            if (obj.IsNew)
            {
                Insert(obj);
            }
            else
            {
                Update(obj);
            }
        }

        #endregion private 结束

    }
}
