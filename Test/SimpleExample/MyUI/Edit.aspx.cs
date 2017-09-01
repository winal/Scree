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
    public partial class Edit : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                foreach (NewsType item in Enum.GetValues(typeof(NewsType)))
                {
                    drpType.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
                }

                GetNewsInDTO inDto = new GetNewsInDTO();
                inDto.Id = Request.QueryString["id"];

                var outDto = NewsService.GetNews(inDto);
                if (!outDto.IsSucceed)
                {
                    LogProxy.Error(outDto.ErrorMsg);
                    Response.Redirect("/index.aspx");
                }

                drpType.SelectedValue = ((int)outDto.Type).ToString();
                txtTitle.Text = outDto.Title;
                txtAuthor.Text = outDto.Author;
                txtContext.Text = outDto.Context;
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            AlterNewsInDTO inDto = new AlterNewsInDTO();
            inDto.Id = Request.QueryString["id"];
            inDto.Type = (NewsType)int.Parse(drpType.SelectedValue);
            inDto.Title = txtTitle.Text.Trim();
            inDto.Author = txtAuthor.Text.Trim();
            inDto.Context = txtContext.Text.Trim();

            var outDto = NewsService.AlterNews(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteNewsInDTO inDto = new DeleteNewsInDTO();
            inDto.Id = Request.QueryString["id"];

            var outDto = NewsService.DeleteNews(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }

    }
}