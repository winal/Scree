using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections;
using System.Data.Common;
using System.Collections.Generic;

namespace Scree.DataBase
{
    public interface IDbOperate
    {
        #region IDBOperate 成员，非存储过程 开始

        int ExecuteNonQuery(string sql, IMyDbParameter[] prams);

        int ExecuteNonQuery(string sql);

        object ExecuteScalar(string sql, IMyDbParameter[] prams);

        object ExecuteScalar(string sql);

        bool IsExist(string sql, IMyDbParameter[] prams);

        bool IsExist(string sql);

        IDataReader GetDataReader(string sql, IMyDbParameter[] prams);

        IDataReader GetDataReader(string sql);

        DataSet GetDataSet(string sql, IMyDbParameter[] prams);

        DataSet GetDataSet(string sql);

        #endregion IDBOperate 成员，非存储过程 结束


        #region IDBOperate 成员，存储过程 开始

        void RunProcedure(string procName, IMyDbParameter[] prams, out Dictionary<string, object> returnValue);

        void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet, out Dictionary<string, object> returnValue);

        //void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader, out Dictionary<string, object> returnValue);

        int RunProcedure(string procName, IMyDbParameter[] prams);

        void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet);

        void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader);

        int RunProcedure(string procName);

        void RunProcedure(string procName, out DataSet dataSet);

        void RunProcedure(string procName, out IDataReader dataReader);

        #endregion IDBOperate 成员，存储过程 结束

    }
}
