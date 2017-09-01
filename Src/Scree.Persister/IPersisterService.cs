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

namespace Scree.Persister
{
    public enum LoadType
    {
        CacheFirst = 0,
        DataBaseDirect = 1,
    }

    public delegate void BeforeSave(SRO[] objs);
    public delegate void AfterSave(SRO[] objs);

    /// <summary>
    /// 对象持久化操作
    /// </summary>
    public interface IPersisterService : IService
    {
        T CreateObject<T>() where T : SRO, new();


        #region 保存对象 开始

        void SaveObject(SRO obj);

        void SaveObject(SRO[] objs);

        #endregion 保存对象 开始


        #region 获得一组对象 开始

        T[] LoadObjects<T>(string alias, string tableName, int top, string condition, IMyDbParameter[] prams, bool isLock, LoadType loadType) where T : SRO, new();
        T[] LoadObjects<T>(string alias, int top, string condition, IMyDbParameter[] prams, bool isLock) where T : SRO, new();
        T[] LoadObjects<T>(string alias, string condition, IMyDbParameter[] prams) where T : SRO, new();
        T[] LoadObjects<T>(string condition, IMyDbParameter[] prams) where T : SRO, new();

        T[] LoadObjects<T>(string alias, int top, string condition, IMyDbParameter[] prams, bool isLock, LoadType loadType) where T : SRO, new();
        T[] LoadObjects<T>(string alias, string condition, IMyDbParameter[] prams, LoadType loadType) where T : SRO, new();
        T[] LoadObjects<T>(string condition, IMyDbParameter[] prams, LoadType loadType) where T : SRO, new();

        #endregion 获得一组对象 结束


        #region 获得单个对象 开始

        T LoadObject<T>(string alias, string tableName, string id, LoadType loadType, bool isNolock) where T : SRO, new();
        T LoadObject<T>(string id, LoadType loadType) where T : SRO, new();
        T LoadObject<T>(string alias, string id, LoadType loadType) where T : SRO, new();
        T LoadObject<T>(string alias, string id, bool isNolock) where T : SRO, new();
        T LoadObject<T>(string alias, string id) where T : SRO, new();
        T LoadObject<T>(string id) where T : SRO, new();

        #endregion 获得单个对象 结束

        void RegisterBeforeSaveMothed(BeforeSave beforeSave);
        void RegisterAfterSaveMothed(AfterSave afterSave);
    }
}
