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
using Scree.Syn.Client;
using System.Threading;
using Scree.Core.IoC;
using Newtonsoft.Json;

namespace Scree.Persister
{
    /// <summary>
    /// 对象持久化操作
    /// </summary>
    public sealed class PersisterService : ServiceBase, IPersisterService
    {
        private static ICacheService CacheService
        {
            get
            {
                return ServiceRoot.GetService<ICacheService>();
            }
        }

        private static IMappingService MappingService
        {
            get
            {
                return ServiceRoot.GetService<IMappingService>();
            }
        }

        private static ISynClientService SynClientService
        {
            get
            {
                return ServiceRoot.GetService<ISynClientService>();
            }
        }


        private BeforeSave BeforeSaveMothed;
        private AfterSave AfterSaveMothed;

        public void RegisterBeforeSaveMothed(BeforeSave beforeSave)
        {
            BeforeSaveMothed = beforeSave;
        }
        public void RegisterAfterSaveMothed(AfterSave afterSave)
        {
            AfterSaveMothed = afterSave;
        }

        private const string SERVICEINITFAIL = "PersisterService init fail, because {0} is null";
        private const string OBJECTISNULL = "Object for save is null";
        private const string OBJECTSISNULL = "Objects for save is null or it's size is zero";
        private const string OBJECTSISNOTMODIFIED = "Object is not modified. Type:{0}, Id:{1}";
        private const string IDISNULL = "Id is null when get single object. Type:{0}";
        private const string DATATYPEERROR = "{0} data type is unallowed";
        private const string SELECTSINGLEOBJECT = "SELECT * FROM [{0}] WHERE Id = @Id And IsDeleted=0";
        private const string SELECTSINGLEOBJECTWITHNOLOCK = "SELECT * FROM [{0}] WITH(NOLOCK) WHERE Id = @Id And IsDeleted=0";
        private const string SELECTOBJECTSHASCONDITION = "SELECT {0} * FROM [{1}] WHERE IsDeleted=0 And {2}";
        private const string SELECTOBJECTS = "SELECT {0} * FROM [{1}] WHERE IsDeleted=0";
        private const string SELECTOBJECTSWITHNOLOCKHASCONDITION = "SELECT {0} * FROM [{1}] WITH(NOLOCK) WHERE IsDeleted=0 And {2}";
        private const string SELECTOBJECTSWITHNOLOCK = "SELECT {0} * FROM [{1}] WITH(NOLOCK) WHERE IsDeleted=0";
        private const string SAVEFAIL = "Save fail, SQL: {0}，prams：";
        private const string LENGTHGAUGE = "Length gauge, {0}: {1}";

        public T CreateObject<T>() where T : SRO, new()
        {
            T obj = (T)Activator.CreateInstance(typeof(T));

            return obj;
        }


        #region 保存对象 开始

        public void SaveObject(SRO obj)
        {
            if (obj == null)
            {
                LogProxy.Warn(OBJECTISNULL);
                return;
            }

            try
            {
                if (BeforeSaveMothed != null)
                {
                    BeforeSaveMothed(new SRO[] { obj });
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
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

            AddCache(obj);

            if (!obj.IsNew && obj.SaveMode != SROSaveMode.NoChange && SynClientService != null)
            {
                SynClientService.Add(obj);
            }

            obj.IsNew = false;

            if (obj.SaveMode != SROSaveMode.NoChange)
            {
                try
                {
                    obj.AfterSave();
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }

                try
                {
                    if (AfterSaveMothed != null)
                    {
                        AfterSaveMothed(new SRO[] { obj });
                    }
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }
        }

        public void SaveObject(SRO[] objs)
        {
            if (objs == null || objs.Length == 0)
            {
                LogProxy.Warn(OBJECTSISNULL);
                return;
            }

            try
            {
                if (BeforeSaveMothed != null)
                {
                    BeforeSaveMothed(objs);
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
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

            bool isChanged = false;
            foreach (SRO obj in objDistincted)
            {
                if (obj != null)
                {
                    AddCache(obj);

                    if (!obj.IsNew && obj.SaveMode != SROSaveMode.NoChange && SynClientService != null)
                    {
                        SynClientService.Add(obj);
                    }

                    obj.IsNew = false;

                    if (obj.SaveMode != SROSaveMode.NoChange)
                    {
                        isChanged = true;

                        try
                        {
                            obj.AfterSave();
                        }
                        catch (Exception ex)
                        {
                            LogProxy.Error(ex, false);
                        }
                    }
                }
            }

            if (isChanged)
            {
                try
                {
                    if (AfterSaveMothed != null)
                    {
                        AfterSaveMothed(objs);
                    }
                }
                catch (Exception ex)
                {
                    LogProxy.Error(ex, false);
                }
            }
        }

        #endregion 保存对象 开始


        #region 获得一组对象 开始

        public T[] LoadObjects<T>(string alias, string tableName, int top, string condition, IMyDbParameter[] prams, bool isLock, LoadType loadType) where T : SRO, new()
        {
            IDataReader dataReader = null;
            try
            {
                string cacheId = null;
                bool needCache = false;

                if (loadType == LoadType.CacheFirst && CacheService != null)
                {
                    needCache = CacheService.IsNeedCached<ArrayCache<T>>();
                    if (needCache)
                    {
                        string jsonPrams = JsonConvert.SerializeObject(prams);
                        cacheId = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", alias, tableName, top, condition, jsonPrams, isLock);
                        cacheId = Tools.MD5(cacheId);

                        T[] objs = LoadCacheObjects<T>(cacheId);
                        if (objs != null)
                        {
                            return objs;
                        }
                    }
                }

                Type type = typeof(T);
                string format;
                if (isLock)
                {
                    if (string.IsNullOrEmpty(condition))
                    {
                        format = SELECTOBJECTS;
                    }
                    else
                    {
                        format = SELECTOBJECTSHASCONDITION;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(condition))
                    {
                        format = SELECTOBJECTSWITHNOLOCK;
                    }
                    else
                    {
                        format = SELECTOBJECTSWITHNOLOCKHASCONDITION;
                    }
                }

                string sql = string.Format(format,
                    (top <= 0 ? "" : "TOP " + top.ToString()),
                    tableName, condition);

                IDbOperate dbOperate = DbProxy.Create(alias, type);
                dataReader = dbOperate.GetDataReader(sql, prams);

                List<T> list = new List<T>();
                while (dataReader.Read())
                {
                    list.Add(LoadObject<T>(dataReader));
                }

                T[] t = list.ToArray();
                if (needCache)
                {
                    AddCache<T>(cacheId, t);
                }
                return t;
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
        public T[] LoadObjects<T>(string alias, int top, string condition, IMyDbParameter[] prams, bool isLock) where T : SRO, new()
        {
            return LoadObjects<T>(alias, typeof(T).Name, top, condition, prams, isLock, LoadType.CacheFirst);
        }
        public T[] LoadObjects<T>(string alias, string condition, IMyDbParameter[] prams) where T : SRO, new()
        {
            return LoadObjects<T>(alias, 0, condition, prams, false);
        }
        public T[] LoadObjects<T>(string condition, IMyDbParameter[] prams) where T : SRO, new()
        {
            return LoadObjects<T>(null, 0, condition, prams, false);
        }

        public T[] LoadObjects<T>(string alias, int top, string condition, IMyDbParameter[] prams, bool isLock, LoadType loadType) where T : SRO, new()
        {
            return LoadObjects<T>(alias, typeof(T).Name, top, condition, prams, isLock, loadType);
        }
        public T[] LoadObjects<T>(string alias, string condition, IMyDbParameter[] prams, LoadType loadType) where T : SRO, new()
        {
            return LoadObjects<T>(alias, 0, condition, prams, false, loadType);
        }
        public T[] LoadObjects<T>(string condition, IMyDbParameter[] prams, LoadType loadType) where T : SRO, new()
        {
            return LoadObjects<T>(null, 0, condition, prams, false, loadType);
        }

        #endregion 获得一组对象 结束


        #region 获得单个对象 开始
        private T LoadObjectDataBaseDirect<T>(string alias, string tableName, string id, bool isNolock) where T : SRO, new()
        {
            Type type = typeof(T);

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
                    return default(T);
                }

                T obj = LoadObject<T>(dataReader);
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

        public T LoadObject<T>(string alias, string tableName, string id, LoadType loadType, bool isNolock) where T : SRO, new()
        {
            if (string.IsNullOrEmpty(id))
            {
                LogProxy.WarnFormat(IDISNULL, typeof(T).ToString());
                return default(T);
            }

            if (loadType == LoadType.CacheFirst)
            {
                T obj = LoadCacheObject<T>(id);

                if (obj != null)
                {
                    if (SynClientService == null)
                    {
                        return obj;
                    }

                    long version;
                    bool isNeedSyn = SynClientService.Peek<T>(id, out version);
                    if (!isNeedSyn || obj.Version >= version)
                    {
                        return obj;
                    }

                    bool isLazy = SynClientService.IsLazy<T>();
                    if (isLazy)
                    {
                        Thread t = new Thread(SynLoadObject<T>);
                        t.Start(new DbInfo(alias, tableName, id, isNolock));

                        return obj;
                    }
                }
            }

            return LoadObjectDataBaseDirect<T>(alias, tableName, id, isNolock);
        }

        private void SynLoadObject<T>(object obj) where T : SRO, new()
        {
            DbInfo info = obj as DbInfo;

            if (info == null)
            {
                return;
            }

            LoadObjectDataBaseDirect<T>(info.Alias, info.TableName, info.Id, info.IsNolock);
        }

        private class DbInfo
        {
            public string Alias { get; private set; }
            public string TableName { get; private set; }
            public string Id { get; private set; }
            public bool IsNolock { get; private set; }

            public DbInfo(string alias, string tableName, string id, bool isNolock)
            {
                this.Alias = alias;
                this.TableName = tableName;
                this.Id = id;
                this.IsNolock = isNolock;
            }
        }

        public T LoadObject<T>(string alias, string id, LoadType loadType) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, loadType, false);
        }
        public T LoadObject<T>(string id, LoadType loadType) where T : SRO, new()
        {
            return LoadObject<T>(null, typeof(T).Name, id, loadType, false);
        }
        public T LoadObject<T>(string alias, string id, bool isNolock) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, LoadType.CacheFirst, isNolock);
        }
        public T LoadObject<T>(string alias, string id) where T : SRO, new()
        {
            return LoadObject<T>(alias, typeof(T).Name, id, LoadType.CacheFirst, false);
        }
        public T LoadObject<T>(string id) where T : SRO, new()
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
                obj.SaveMode = SROSaveMode.NoChange;
                LogProxy.InfoFormat(OBJECTSISNOTMODIFIED, obj.GetType(), obj.Id);
                return;
            }

            obj.SaveMode = SROSaveMode.Update;

            Save(obj, updateSql, prams);
        }

        private static void Save(SRO obj, string sql, IMyDbParameter[] prams)
        {
            string tableName;
            string executeSql;
            int executeCnt;
            Type type = obj.GetType();

            string[] aliases = obj.Aliases;
            IDbOperate dbOperate;
            if (aliases.Length == 0)
            {
                tableName = GetTableName(obj, null);
                executeSql = string.Format(sql, tableName);

                dbOperate = DbProxy.Create(null, type);
                executeCnt = dbOperate.ExecuteNonQuery(executeSql, prams);
                if (executeCnt != 1)
                {
                    string jsonPrams = JsonConvert.SerializeObject(prams);
                    LogProxy.Error(string.Format(SAVEFAIL, executeSql, jsonPrams), true);
                }
            }
            else
            {
                foreach (string alias in aliases)
                {
                    tableName = GetTableName(obj, alias);
                    executeSql = string.Format(sql, tableName);

                    dbOperate = DbProxy.Create(alias, type);
                    executeCnt = dbOperate.ExecuteNonQuery(executeSql, prams);
                    if (executeCnt != 1)
                    {
                        string jsonPrams = JsonConvert.SerializeObject(prams);
                        LogProxy.Error(string.Format(SAVEFAIL, executeSql, jsonPrams), true);
                    }
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
                            string val = pValue as string;
                            if (!string.IsNullOrEmpty(val) && val.Length > property.StringDataType.Length)
                            {
                                LogProxy.Error(string.Format(LENGTHGAUGE, property.Name, val), true);
                            }

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
                    if (property.IsSave == false || property.Name == "CreatedDate" || property.Name == "Version")
                    {//判断是否保存
                        continue;
                    }

                    object pValue = property.GetValue(obj);
                    if (property.Name == "Id" || property.Name == "LastAlterDate")
                    {
                        pramList.Add(ParameterBuilder(property, pValue));
                        continue;
                    }

                    if (property.PropertyType == typeof(DateTime) && (DateTime)pValue == DateTime.MinValue && obj.GetOriginalValue(property.Name) == null)
                    {
                        continue;
                    }
                    else if (object.Equals(obj.GetOriginalValue(property.Name), pValue))
                    {
                        continue;
                    }

                    if (!isModified)
                    {
                        isModified = true;
                    }

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
                sql.Append(string.Format("[LastAlterDate]=@LastAlterDate,[Version]={0} WHERE [Id]=@Id AND [Version]={1}",
                    ++obj.Version, (long)obj.GetOriginalValue("Version")));

                return sql.ToString();
            }
        }

        private T LoadObject<T>(IDataReader dataReader) where T : SRO, new()
        {
            T obj = CreateObject<T>();
            Type type = typeof(T);

            IMyPropertyInfo[] myPropertys = PropertyInfoProxy.GetProperties(type);
            foreach (IMyPropertyInfo property in myPropertys)
            {
                if (property.IsLoad == false && property.IsSave == false)
                {//判断是否加载
                    continue;
                }

                object value = dataReader[property.Name];

                if (value == DBNull.Value)
                {
                    continue;
                }

                if (property.IsLoad)
                {
                    property.SetValue(obj, value);
                }

                if (property.PropertyType.IsEnum)
                {
                    obj.SetOriginalValue(property.Name, Enum.Parse(property.PropertyType, value.ToString()));
                }
                else
                {
                    obj.SetOriginalValue(property.Name, value);
                }
            }

            IMyPropertyInfo isNew = PropertyInfoProxy.GetProperty(type, "IsNew");
            isNew.SetValue(obj, false);

            AddCache(obj);

            return obj;
        }

        private static void AddCache(SRO obj)
        {
            try
            {
                if (CacheService != null)
                {
                    obj.Bestrow();

                    CacheService.Set(obj);
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static void AddCache<T>(string id, T[] objs) where T : ICacheable, new()
        {
            try
            {
                if (CacheService == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(id))
                {
                    return;
                }

                ArrayCache<T> arrayCache = new ArrayCache<T>();

                arrayCache.Id = id;
                arrayCache.Objects = objs;
                arrayCache.Version = DateTime.Now.Ticks;

                CacheService.Set(arrayCache);

            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        private static T[] LoadCacheObjects<T>(string id) where T : ICacheable
        {
            try
            {
                if (CacheService == null)
                {
                    return null;
                }
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }

                ArrayCache<T> arrayCache = CacheService.Get<ArrayCache<T>>(id) as ArrayCache<T>;
                if (arrayCache == null)
                {
                    return null;
                }
                return arrayCache.Objects;
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
                return null;
            }
        }

        private static T LoadCacheObject<T>(string id) where T : SRO
        {
            try
            {
                if (CacheService == null)
                {
                    return null;
                }

                T obj = CacheService.Get<T>(id) as T;
                if (obj == null || obj.IsDeleted)
                {
                    return null;
                }
                return obj;
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
                return null;
            }
        }

        private static void SaveSingleObject(SRO obj)
        {
            obj.BeforeSave();

            obj.LastAlterDate = DateTime.Now;

            if (obj.IsNew)
            {
                obj.SaveMode = SROSaveMode.Insert;
                Insert(obj);
            }
            else
            {
                Update(obj);
            }
        }

        #endregion private 结束


        public override bool Init()
        {
            if (MappingService == null)
            {
                LogProxy.FatalFormat(SERVICEINITFAIL, "MappingService", true);
            }

            if (!MappingService.IsInitialized)
            {
                return false;
            }

            if (CacheService != null && !CacheService.IsInitialized)
            {
                return false;
            }

            if (SynClientService != null && !SynClientService.IsInitialized)
            {
                return false;
            }

            return DbTableService.Init();
        }
    }
}
