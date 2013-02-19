<%@ Page Language="C#" Inherits="VingdWebExample.Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head runat="server">
	<title>VingdWebExample</title>
	
    <script type="text/javascript" src="http://apps.vingd.com/cdn/vingd-popup/v0.8/build/main.min.js"></script>
    <script type="text/javascript">
		var siteURL = "<%= baseURL %>";
		var objectURL = siteURL + "/?state=access";
		var frontendURL = "<%= vingdFrontendURL %>";
			
        // popup purchase
        var orderOpener = new vingd.popupOpener({
            popupURL: siteURL+"/popup.html",
            frontendURL: frontendURL,
            siteURL: siteURL,
			lang: 'en',
            onSuccess: function(hwnd, args) {
                window.location = vingd.buildURL(objectURL, {token: args.token, context: args.context});
            }
        });
        
        // popup rewarding (voucher)
        var voucherOpener = new vingd.popupOpener({
            popupURL: siteURL+"/popup.html",
            frontendURL: frontendURL,
            siteURL: siteURL,
			lang: 'en',
			onSuccess: function() {
				document.getElementById("msg").innerHTML = "Voucher cashed-in.";
			}
        });
	</script>

</head>
<body style="font-family: sans-serif;">

<asp:Label id="lblResponse" runat="server" Text="" Style="display: block; white-space: pre; margin: 16px 0; font-family: monospace; font-size: 12px;" OnLoad="btnRequest_Load"></asp:Label>
<asp:HyperLink id="linkAction" runat="server"></asp:HyperLink>
<asp:HyperLink id="linkActionAlt" runat="server" Style="margin-left: 16px;"></asp:HyperLink>
<div style="margin: 16px 0" id="msg"></div>
		
<div style="margin-top: 32px"><a href="/">Back to home</a></div>
<hr/>
<span style="font-size: smaller">(debugging) Execute a custom backend request:</span>
<form id="form1" runat="server" style="margin: 8px 0;" action="/?state=custom-get-request">
	<asp:TextBox runat="server" id="txtUrl" Style="width: 400px"></asp:TextBox>
	<asp:Button id="btnRequest" runat="server" Text="request" /> <br/>
	<pre>
  Try:
    /registry/objects/
    /registry/objects/&lt;oid&gt;
    /id/users/
    /fort/accounts
    /fort/transfers
	</pre>
</form>

</body>
</html>
