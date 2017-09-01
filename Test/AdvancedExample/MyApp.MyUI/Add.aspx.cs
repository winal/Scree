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
    public partial class Add : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                foreach (Gender item in Enum.GetValues(typeof(Gender)))
                {
                    drpGender.Items.Add(new ListItem(item.ToString(), ((int)item).ToString()));
                }
            }
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            AddUserInDTO inDto = new AddUserInDTO();
            inDto.Name = txtName.Text.Trim();
            inDto.Gender = (Gender)int.Parse(drpGender.SelectedValue);
            inDto.Mobile = txtMobile.Text.Trim();
            inDto.AvailableBalance = decimal.Parse(txtAvailableBalance.Text.Trim());

            var outDto = UserService.AddUser(inDto);
            if (!outDto.IsSucceed)
            {
                LogProxy.Error(outDto.ErrorMsg);
            }

            Response.Redirect("/index.aspx");
        }


    }
}