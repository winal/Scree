using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Collections;
using Scree.Log;
using System.Collections.Generic;

namespace Scree.DataBase
{
    internal sealed class SQLServer : IDbOperate
    {
        private const string TOSTRING = "Alias:{0}; Type:{1}; Assembly:{2}; StorageContext:{3}";
        private const string SQLPRAMSDBOPERATE = "SQL:{0}; PRAMS:{1}; DbOperate:{2}";
        private const string SQLDBOPERATE = "SQL:{0}; DbOperate:{1}";
        private static Dictionary<string, string> ConnectionStrings = new Dictionary<string, string>(29);
        private StorageContext Context { get; set; }
        internal string Alias { get; set; }
        internal Type ObjType { get; set; }
        private int Timeout { get; set; }
        private string ConnectionString
        {
            get
            {
                if (ConnectionStrings.ContainsKey(Context.Name))
                {
                    _connectionString = ConnectionStrings[Context.Name];
                }

                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = ConnectionStringBuilder();

                    lock (ConnectionStrings)
                    {
                        if (ConnectionStrings.ContainsKey(Context.Name) == false)
                        {
                            ConnectionStrings[Context.Name] = _connectionString;
                        }
                    }
                }

                return _connectionString;
            }
        }
        private string _connectionString;
        private SqlConnection Connection { get; set; }

        public SQLServer(StorageContext context)
        {
            this.Context = context;
        }


        #region 数据库连接和关闭 开始

        private void Open()
        {
            if (Connection == null)
            {
                Connection = new SqlConnection(ConnectionString);
                Connection.Open();
            }
            else if (Connection.State == ConnectionState.Closed)
            {
                Connection.Open();
            }
        }

        private void Close()
        {
            if (Connection != null)
            {
                Connection.Close();
            }
        }

        private void Dispose()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }

        #endregion 数据库连接和关闭 结束


        #region IDBOperate 成员，非存储过程 开始

        public int ExecuteNonQuery(string sql, IMyDbParameter[] prams)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text, prams);
                int i = scm.ExecuteNonQuery();

                scm.Parameters.Clear();
                return i;
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, sql, GetParametersInfo(prams), this.ToString());
                return 0;
            }
            finally
            {
                Close();
            }
        }

        public int ExecuteNonQuery(string sql)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text);
                return scm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, sql, this.ToString());
                return 0;
            }
            finally
            {
                Close();
            }
        }

        public object ExecuteScalar(string sql, IMyDbParameter[] prams)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text, prams);
                object obj = scm.ExecuteScalar();

                return obj;
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, sql, GetParametersInfo(prams), this.ToString());
                return null;
            }
            finally
            {
                Close();
            }
        }

        public object ExecuteScalar(string sql)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text);
                object obj = scm.ExecuteScalar();

                return obj;
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, sql, this.ToString());
                return null;
            }
            finally
            {
                Close();
            }
        }

        public bool IsExist(string sql, IMyDbParameter[] prams)
        {
            return ExecuteScalar(sql, prams) != null;
        }

        public bool IsExist(string sql)
        {
            return ExecuteScalar(sql) != null;
        }

        public IDataReader GetDataReader(string sql, IMyDbParameter[] prams)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text, prams);
                return scm.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, sql, GetParametersInfo(prams), this.ToString());
                return null;
            }
        }

        public IDataReader GetDataReader(string sql)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text);
                return scm.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, sql, this.ToString());
                return null;
            }
        }

        public DataSet GetDataSet(string sql, IMyDbParameter[] prams)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text, prams);

                DataSet dataSet;
                FillDataSet(out dataSet, scm);

                return dataSet;
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, sql, GetParametersInfo(prams), this.ToString());
                return null;
            }
            finally
            {
                Close();
            }
        }

        public DataSet GetDataSet(string sql)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(sql, CommandType.Text);

                DataSet dataSet;
                FillDataSet(out dataSet, scm);

                return dataSet;
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, sql, this.ToString());
                return null;
            }
            finally
            {
                Close();
            }
        }

        #endregion IDBOperate 成员，非存储过程 结束


        #region IDBOperate 成员，存储过程 开始

        public void RunProcedure(string procName, IMyDbParameter[] prams, out Dictionary<string, object> returnValue)
        {
            returnValue = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);
                scm.ExecuteNonQuery();
                FillValueContainer(prams, scm, out returnValue);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
            }
            finally
            {
                Close();
            }
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet, out Dictionary<string, object> returnValue)
        {
            dataSet = null;
            returnValue = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);

                FillDataSet(out dataSet, scm);

                FillValueContainer(prams, scm, out returnValue);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
            }
            finally
            {
                Close();
            }
        }

        //public void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader, out Dictionary<string, object> returnValue)
        //{
        //    dataReader = null;
        //    returnValue = null;

        //    try
        //    {
        //        Open();

        //        SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);

        //        dataReader = scm.ExecuteReader(CommandBehavior.CloseConnection);
        //        dataReader.Close();
        //        FillValueContainer(prams, scm, out returnValue);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
        //    }
        //}

        public int RunProcedure(string procName, IMyDbParameter[] prams)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);

                return scm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
                return 0;
            }
            finally
            {
                Close();
            }
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out DataSet dataSet)
        {
            dataSet = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);

                FillDataSet(out dataSet, scm);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
            }
            finally
            {
                Close();
            }
        }

        public void RunProcedure(string procName, IMyDbParameter[] prams, out IDataReader dataReader)
        {
            dataReader = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure, prams);

                dataReader = scm.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLPRAMSDBOPERATE, procName, GetParametersInfo(prams), this.ToString());
            }
        }

        public int RunProcedure(string procName)
        {
            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure);

                return scm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, procName, this.ToString());
                return 0;
            }
            finally
            {
                Close();
            }
        }

        public void RunProcedure(string procName, out DataSet dataSet)
        {
            dataSet = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure);

                FillDataSet(out dataSet, scm);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, procName, this.ToString());
            }
            finally
            {
                Close();
            }
        }

        public void RunProcedure(string procName, out IDataReader dataReader)
        {
            dataReader = null;

            try
            {
                Open();

                SqlCommand scm = CreateCommand(procName, CommandType.StoredProcedure);

                dataReader = scm.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                LogProxy.ErrorFormat(ex, true, SQLDBOPERATE, procName, this.ToString());
            }
        }

        #endregion IDBOperate 成员，存储过程 结束


        #region 自定义方法 开始

        private static string GetParametersInfo(IMyDbParameter[] prams)
        {
            if (prams == null || prams.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            foreach (IMyDbParameter pram in prams)
            {
                sb.Append(pram.ToString());
            }

            return sb.ToString();
        }

        private string ConnectionStringBuilder()
        {
            StringBuilder connection = new StringBuilder("data source=");
            connection.Append(Context.DataSource);
            connection.Append(";initial catalog=");
            connection.Append(Context.Catalog);
            connection.Append(";uid=");
            connection.Append(Context.UId);
            connection.Append(";pwd=");
            connection.Append(Context.Pwd);
            if (Context.MaxPoolSize != 0)
            {
                connection.Append(";max pool size=");
                connection.Append(Context.MaxPoolSize.ToString());
            }
            if (Context.MinPoolSize != 0)
            {
                connection.Append(";min pool size=");
                connection.Append(Context.MinPoolSize.ToString());
            }
            connection.Append(";pooling=true;");
            return connection.ToString();
        }

        private static void FillValueContainer(IMyDbParameter[] prams, SqlCommand scm, out Dictionary<string, object> returnValue)
        {
            if (prams == null || prams.Length == 0)
            {
                returnValue = null;
                return;
            }

            returnValue = new Dictionary<string, object>();
            foreach (IMyDbParameter parameter in prams)
            {
                switch (parameter.Direction)
                {
                    case ParameterDirection.Output:
                        returnValue[parameter.ParameterName] = scm.Parameters[parameter.ParameterName].Value;
                        break;
                }
            }
        }

        private static void FillDataSet(out DataSet dataSet, SqlCommand scm)
        {
            SqlDataAdapter sda = new SqlDataAdapter(scm);
            dataSet = new DataSet();
            sda.Fill(dataSet);
        }

        private SqlCommand CreateCommand(string cmdText, CommandType cmdType, IMyDbParameter[] prams)
        {
            SqlCommand cmd = new SqlCommand(cmdText, this.Connection);
            cmd.CommandTimeout = this.Timeout;
            cmd.CommandType = cmdType;

            // 依次把参数传入存储过程
            if (prams != null && prams.Length != 0)
            {
                foreach (IMyDbParameter parameter in prams)
                {
                    cmd.Parameters.Add(parameter.Para);
                }
            }

            return cmd;
        }

        private SqlCommand CreateCommand(string cmdText, CommandType cmdType)
        {
            SqlCommand cmd = new SqlCommand(cmdText, this.Connection);
            cmd.CommandTimeout = this.Timeout;
            cmd.CommandType = cmdType;

            return cmd;
        }

        #endregion 自定义方法 结束

        public override string ToString()
        {
            return string.Format(TOSTRING, Alias, ObjType == null ? "" : ObjType.FullName,
                ObjType == null ? "" : ObjType.Assembly.FullName, Context.Name);
        }
    }
}