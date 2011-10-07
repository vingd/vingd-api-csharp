<%@ Page Language="C#" Inherits="KnopsoAPIExample.Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head runat="server">
	<title>Default</title>
	
    <!-- purchase-in-popup magical part -->
    <script type="text/javascript" src="knopso-popup/knopso.min.js"></script>
    <script type="text/javascript">
    	var home = "<%= baseURL %>";
    	var objecthome = home + "/?state=access";
        var sell = new knopso.popupOpener({
            epiloguePage: home+"/knopso-popup/epilogue.html",
            onSuccess: function(hwnd, token) {
                window.location = objecthome + "&token="+token;
            }
        });
    </script>

</head>
<body style="font-family: sans-serif;">
<form id="form1" runat="server">
	<asp:TextBox runat="server" id="txtUrl" Style="width: 400px"></asp:TextBox>
	<asp:Button id="btnRequest" runat="server" Text="request" OnClick="btnRequest_Click" /> <br/>
	<pre>
	Try:
	/registry/objects/
	/registry/objects/&lt;oid&gt;
	/id/users/
	/fort/accounts
	/fort/transfers
	</pre>
	<hr/>
</form>
<asp:Label id="lblResponse" runat="server" Text="" Style="display: block; white-space: pre; margin: 16px 0; font-family: monospace; font-size: 12px;" OnLoad="btnRequest_Load"></asp:Label>
<asp:HyperLink id="linkAction" runat="server"></asp:HyperLink>
<asp:HyperLink id="linkActionAlt" runat="server" Style="margin-left: 16px;"></asp:HyperLink>

</body>
</html>
