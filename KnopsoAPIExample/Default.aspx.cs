using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Knopso;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KnopsoAPIExample {
	
	public partial class Default : System.Web.UI.Page {
		public const string baseURL = "http://127.0.0.1:8080";
		
		public const string knopsoBackendURL = KnopsoBroker.sandboxEndpointURL;
		public const string knopsoFrontendURL = KnopsoBroker.sandboxFrontendURL;
		
		private const string knopsoUsername = "test@vingd.com";
		private const string knopsoPassword = "123";
		private KnopsoBroker knopso = null;

		public Default() {
			string knopsoPasswordHash = KnopsoBroker.SHA1(knopsoPassword);
			
			// during development and testing, use sandbox:
			knopso = new KnopsoBroker(knopsoUsername, knopsoPasswordHash, knopsoBackendURL, knopsoFrontendURL);
			
			// in production, use:
			//knopso = new KnopsoBroker(knopsoUsername, knopsoPasswordHash);
		}

		private void LogAppend(string msg) {
			lblResponse.Text += msg + "\n";
		}
		
		private void LogReplace(string msg) {
			lblResponse.Text = msg + "\n";
		}
		
		private void SetLink(HyperLink elem, string text, string url) {
			elem.Text = text;
			elem.NavigateUrl = url;
		}
		
		private string ObjectDump(object data) {
			return new JsonFormatter(knopso.JSONStringify(data)).Format();
		}

		
		private void HandlePageRequest() {
			switch (Request["state"]) {
			
			case "register":
				Session["oid"] = knopso.RegisterObject("C# test object", baseURL+"/?state=access");
				LogAppend("Object registered, Object ID = " + Session["oid"]);
				SetLink(linkAction, "Create an order for this object.", "/?state=order");
				break;
			
			case "order":
				if (Session["oid"] == null) {
					SetLink(linkAction, "Register an object first.", "/?state=register");
				} else {
					KnopsoOrder order = knopso.CreateOrder((long)Session["oid"], 1.99);
					Session["orderid"] = order.id;
					LogAppend("Order created, Order ID = " + order.id);
					SetLink(linkAction, "Buy it!", order.GetRedirectURL("my-custom-context"));
					SetLink(linkActionAlt, "Buy it in a popup!", order.GetRedirectURL("my-custom-context"));
					linkActionAlt.Attributes["onclick"] = "return orderOpener(this);";
				}
				break;
			
			case "access":
				string context = Request["context"];
				string token = Request["token"];
				KnopsoPurchase purchase = knopso.VerifyToken(token);
				LogAppend("Context: " + context);
				LogAppend("Purchase: " + ObjectDump(purchase));
				// serve the content, then commit
				LogAppend("TODO: serve the content.");
				knopso.CommitPurchase(purchase);
				LogAppend("Purchase committed.");
				// test direct rewarding:
				knopso.RewardUser(purchase.huid, 1.25, "Custom transaction description");
				LogAppend("User rewarded.");
				break;
			
			case "custom-get-request":
				object data = knopso.Request("GET", txtUrl.Text, null);
				LogAppend("Response: " + ObjectDump(data));
				break;
			
			case "voucher":
				KnopsoVoucher voucher = knopso.CreateVoucher(1.99, "here goes a message for the user");
				LogAppend("Voucher code: " + voucher.code);
				SetLink(linkAction, "Use this voucher!", voucher.GetRedirectURL());
				SetLink(linkActionAlt, "Use this voucher in a popup!", voucher.GetRedirectURL());
				linkActionAlt.Attributes["onclick"] = "return voucherOpener(this);";
				break;
				
			default:
				LogAppend("You can start by registering an object, or creating a voucher.");
				SetLink(linkAction, "Register an object.", "/?state=register");
				SetLink(linkActionAlt, "Create a voucher.", "/?state=voucher");
				break;
			}
		}

		protected void btnRequest_Load (object sender, System.EventArgs e) {
			try {
				HandlePageRequest();
				
			} catch (KnopsoTransportException ex) {
				LogAppend(ex.ToString());
			} catch (KnopsoOperationException ex) {
				LogAppend(ex.ToString());
			} catch (Exception ex) {
				LogAppend("Unhandled exception: " + ex);
			}
		}

	}

}
