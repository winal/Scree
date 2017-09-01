using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyApp.Models;
using Scree.Log;

namespace MyApp
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                drpGender.Items.Add(new ListItem("请选择", "-1"));
                foreach (Gender item in Enum.GetValues(typeof(Gender)))
                {
                    drpGender.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
                }

                OutputList();
            }
        }

        /// <summary>
        /// 查询条件拼接
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static string QueryConditionBuilder(Queue<string> condition)
        {
            if (condition == null || condition.Count == 0)
            {
                return null;
            }

            if (condition.Count == 1)
            {
                return condition.Dequeue();
            }

            StringBuilder query = new StringBuilder();
            foreach (string s in condition)
            {
                query.Append(" AND ");
                query.Append(s);
            }
            query.Remove(0, 5);
            return query.ToString();
        }

        /// <summary>
        /// SQL 防注入过滤
        /// </summary>
        /// <param name="sqlCondition"></param>
        /// <returns></returns>
        private static string SqlFilter(string sqlCondition)
        {
            return sqlCondition.Replace("'", "");
        }
        private string CreateCondition()
        {
            Queue<string> condition = new Queue<string>();

            if (string.IsNullOrEmpty(txtName.Text.Trim()) == false)
            {
                condition.Enqueue(string.Format("Name='{0}'", SqlFilter(txtName.Text.Trim())));
            }

            if (drpGender.SelectedValue != "-1")
            {
                condition.Enqueue(string.Format("Gender='{0}'", SqlFilter(drpGender.SelectedValue.Trim())));
            }

            if (string.IsNullOrEmpty(txtMobile.Text.Trim()) == false)
            {
                condition.Enqueue(string.Format("Mobile like '%{0}%'", SqlFilter(txtMobile.Text.Trim())));
            }

            return QueryConditionBuilder(condition);
        }

        private void OutputList()
        {
            try
            {
                string condition = CreateCondition();

                QueryInDTO inDto = new QueryInDTO();
                inDto.PageSize = 99;
                inDto.PageIndex = 1;
                inDto.Condition = condition;

                QueryOutDTO outDto = QueryService.GetUserList(inDto);
                if (outDto.IsSucceed)
                {
                    ltBody.Text = outDto.Body;
                    ltCnt.Text = outDto.RecordCount.ToString();
                }
                else
                {
                    ltBody.Text = outDto.ErrorMsg;
                }
            }
            catch (Exception ex)
            {
                LogProxy.Error(ex, false);
            }
        }

        protected void btnSelect_Click(object sender, EventArgs e)
        {
            OutputList();
        }


    }
}