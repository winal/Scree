<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="MyUI.Edit" %>

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
            <br />
            标题<asp:TextBox ID="txtTitle" runat="server"></asp:TextBox>
            <br />
            作者<asp:TextBox ID="txtAuthor" runat="server"></asp:TextBox>
            <br />
            内容<asp:TextBox ID="txtContext" TextMode="MultiLine" runat="server"></asp:TextBox>
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
