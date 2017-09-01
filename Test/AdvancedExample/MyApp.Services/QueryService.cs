using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using Scree.Common;
using Scree.Core.IoC;
using Scree.DataBase;
using Scree.Log;
using Scree.Persister;

namespace MyApp.Models
{
    public static class QueryService
    {
        //存储过程内容见：dbbak文件夹
        private const string PROPAGING = "proPaging";
        private static QueryOutDTO QueryConstructor(Type type, string tableName, QueryInDTO inDto, out DataView dataView)
        {
            return QueryConstructor(type, tableName, "CreatedDate DESC", "*", inDto, out dataView);
        }
        private static QueryOutDTO QueryConstructor(Type type, string tableName, string columnList, QueryInDTO inDto, out DataView dataView)
        {
            return QueryConstructor(type, tableName, "CreatedDate DESC", columnList, inDto, out dataView);
        }
        private static QueryOutDTO QueryConstructor(Type type, string tableName, string orderBy, string columnList, QueryInDTO inDto, out DataView dataView)
        {
            QueryOutDTO outDto = new QueryOutDTO();
            dataView = null;
            try
            {
                Dictionary<string, object> returnValue;

                IMyDbParameter[] prams = 
                {
                    DbParameterProxy.Create("@TableName", SqlDbType.NVarChar, 50, tableName),
                    DbParameterProxy.Create("@OrderBy", SqlDbType.NVarChar, 500, orderBy),
                    DbParameterProxy.Create("@ColumnList", SqlDbType.NVarChar, 500, columnList),
                    DbParameterProxy.Create("@PageSize", SqlDbType.Int, inDto.PageSize),
                    DbParameterProxy.Create("@PageIndex", SqlDbType.Int, inDto.PageIndex),
                    DbParameterProxy.Create("@Condition", SqlDbType.NVarChar, 4000, inDto.Condition),
                    DbParameterProxy.Create("@PageCount", SqlDbType.Int, ParameterDirection.Output),
                    DbParameterProxy.Create("@RecordCount", SqlDbType.Int, ParameterDirection.Output)
                };
                DataSet dataSet;

                IDbOperate dbOperate = DbProxy.Create(null, type);
                dbOperate.RunProcedure(PROPAGING, prams, out dataSet, out returnValue);
                dataView = dataSet.Tables[0].DefaultView;

                outDto.IsSucceed = true;
                outDto.PageCount = (int)returnValue["@PageCount"];
                outDto.RecordCount = (int)returnValue["@RecordCount"];
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
                outDto.ErrorMsg = ex.Message;
            }

            return outDto;
        }

        public static QueryOutDTO GetUserList(QueryInDTO inDto)
        {
            QueryOutDTO outDto = new QueryOutDTO();

            DataView dataView;
            outDto = QueryConstructor(typeof(User), "User", "Gender DESC,CreatedDate ASC", "Id,Name,Gender,Mobile,AvailableBalance,CreatedDate", inDto, out dataView);

            if (outDto.IsSucceed == false)
            {
                return outDto;
            }

            StringBuilder body = new StringBuilder();
            //html模板建议做成配置
            body.Append("<table border=1><tr><td>姓名</td><td>性别</td><td>手机</td><td>余额</td><td>创建时间</td><td>管理</td></tr>");

            if (dataView != null && dataView.Count != 0)
            {
                var format = "<tr><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td><a href='edit.aspx?id={0}'>编辑</a></td></tr>";
                for (int i = 0; i < dataView.Count; i++)
                {
                    body.AppendFormat(format, dataView[i]["Id"].ToString(),
                         dataView[i]["Name"].ToString(),
                         ((Gender)dataView[i]["Gender"]).ToString(),
                         dataView[i]["Mobile"].ToString(),
                         dataView[i]["AvailableBalance"].ToString(),
                         ((DateTime)dataView[i]["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss")
                         );
                }
            }

            body.Append("</table>");

            outDto.Body = body.ToString();
            return outDto;
        }
    }
}