<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="MyApp.Index" %>

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
            性别<asp:DropDownList ID="drpGender" runat="server">
            </asp:DropDownList>
            手机<asp:TextBox ID="txtMobile" runat="server"></asp:TextBox>
            &nbsp;<asp:Button ID="btnSelect" runat="server" OnClick="btnSelect_Click" Text="查 询" />
            &nbsp;&nbsp;<a href="add.aspx">新增</a>
            <br />
            <br />
            <asp:Literal runat="server" ID="ltBody"></asp:Literal>
            <br />
            总数：<asp:Literal ID="ltCnt" runat="server"></asp:Literal>
            <br />
        </div>
    </form>
</body>
</html>
