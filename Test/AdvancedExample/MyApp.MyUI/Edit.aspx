<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="MyApp.Edit" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            姓名<asp:TextBox ID="txtName" runat="server"></asp:TextBox>
            <br />
            性别<asp:DropDownList ID="drpGender" runat="server">
            </asp:DropDownList>
            <br />
            手机<asp:TextBox ID="txtMobile" runat="server"></asp:TextBox>
            <br />
            余额<asp:TextBox ID="txtAvailableBalance" runat="server"></asp:TextBox>
            <br />
            <asp:Button ID="btnEdit" runat="server" OnClick="btnEdit_Click" Text="修改" />
            <br />
            <br />
            <asp:Button ID="btnDelete" runat="server" OnClick="btnDelete_Click" Text="删除" />
            <br />
            <br />
        </div>
    </form>
</body>
</html>
