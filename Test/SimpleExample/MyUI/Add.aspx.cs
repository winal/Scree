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
    public partial class Add : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                foreach (NewsType item in Enum.GetValues(typeof(NewsType)))
                {
                    drpType.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
                }
            }
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            AddNewsInDTO inDto = new AddNewsInDTO();
            inDto.Type = (NewsType)int.Parse(drpType.SelectedValue);
            inDto.Title = txtTitle.Text.Trim();
            inDto.Author = txtAuthor.Text.Trim();
            inDto.Context = txtContext.Text.Trim();

            var outDto = NewsService.AddNews(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }


    }
}