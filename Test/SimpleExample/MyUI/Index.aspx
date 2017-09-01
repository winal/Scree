<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="MyUI.Index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            类型<asp:DropDownList ID="drpType" runat="server">
            </asp:DropDownList>
            作者<asp:TextBox ID="txtAuthor" runat="server"></asp:TextBox>
            标题<asp:TextBox ID="txtTitle" runat="server"></asp:TextBox>
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
