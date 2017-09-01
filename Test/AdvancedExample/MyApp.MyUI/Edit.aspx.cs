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
    public partial class Edit : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                foreach (Gender item in Enum.GetValues(typeof(Gender)))
                {
                    drpGender.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
                }

                GetUserInDTO inDto = new GetUserInDTO();
                inDto.Id = Request.QueryString["id"];

                var outDto = UserService.GetUser(inDto);
                if (!outDto.IsSucceed)
                {
                    LogProxy.Error(outDto.ErrorMsg);
                    Response.Redirect("/index.aspx");
                }

                txtName.Text = outDto.Name;
                drpGender.SelectedValue = ((int)outDto.Gender).ToString();
                txtMobile.Text = outDto.Mobile;
                txtAvailableBalance.Text = outDto.AvailableBalance.ToString();
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            AlterUserInDTO inDto = new AlterUserInDTO();
            inDto.Id = Request.QueryString["id"];
            inDto.Name = txtName.Text.Trim();
            inDto.Gender = (Gender)int.Parse(drpGender.SelectedValue);
            inDto.Mobile = txtMobile.Text.Trim();
            inDto.AvailableBalance = decimal.Parse(txtAvailableBalance.Text.Trim());

            var outDto = UserService.AlterUser(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteUserInDTO inDto = new DeleteUserInDTO();
            inDto.Id = Request.QueryString["id"];

            var outDto = UserService.DeleteUser(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }

    }
}