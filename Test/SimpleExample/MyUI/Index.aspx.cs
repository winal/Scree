using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyUI.Models;
using Scree.Log;

namespace MyUI
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                drpType.Items.Add(new ListItem("请选择", "-1"));
                foreach (NewsType item in Enum.GetValues(typeof(NewsType)))
                {
                    drpType.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
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

            if (drpType.SelectedValue != "-1")
            {
                condition.Enqueue(string.Format("[Type]='{0}'", SqlFilter(drpType.SelectedValue.Trim())));
            }

            if (string.IsNullOrEmpty(txtAuthor.Text.Trim()) == false)
            {
                condition.Enqueue(string.Format("Author='{0}'", SqlFilter(txtAuthor.Text.Trim())));
            }

            if (string.IsNullOrEmpty(txtTitle.Text.Trim()) == false)
            {
                condition.Enqueue(string.Format("Title like '%{0}%'", SqlFilter(txtTitle.Text.Trim())));
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

                QueryOutDTO outDto = QueryService.GetNewsList(inDto);
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