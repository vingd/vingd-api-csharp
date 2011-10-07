<%@ Page Language="C#" Inherits="KnopsoAPIExample.Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head runat="server">
	<title>Default</title>
</head>
<body style="font-family: sans-serif;">
<form id="form1" runat="server">
	<asp:TextBox runat="server" id="txtUrl" Style="width: 400px"></asp:TextBox>
	<asp:Button id="btnRequest" runat="server" Text="request" OnClick="btnRequest_Click" OnLoad="btnRequest_Load" /> <br/>
</form>
<asp:Label id="lblResponse" runat="server" Text="" Style="display: block; white-space: pre; margin: 16px 0; font-family: monospace; font-size: 12px;"></asp:Label>
<asp:HyperLink id="linkAction" runat="server"></asp:HyperLink>

</body>
</html>
