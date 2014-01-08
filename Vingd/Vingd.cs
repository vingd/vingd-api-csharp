using System;
using System.Net;
using System.IO;
using System.Text;
using System.Security.Cryptography;

// JSON decoder: JavaScriptSerializer (requires: System.Web.Extensions)
using System.Web.Script.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Vingd {
	
	public class VingdOperationException : Exception {
		protected string context = null;
		protected HttpStatusCode code = HttpStatusCode.Conflict;

		public VingdOperationException(string message, string context) : this(message, context, HttpStatusCode.Conflict, null) {}
		
		public VingdOperationException(string message, string context, HttpStatusCode code) : this(message, context, code, null) {}
		
		public VingdOperationException(string message, string context, HttpStatusCode code, Exception inner) 
		: base(message, inner) {
			this.context = context;
			this.code = code;
		}

		public override string ToString() {
			return this.GetType().Name + ": " + (context != null ? "[" + context + "]: " : "") + Message + " (" + (int)code + " " + code.ToString() + ")";
		}
	}

	
	public class VingdTransportException : Exception {
		protected string context = null;
		protected HttpStatusCode code = HttpStatusCode.Conflict;

		public VingdTransportException(string message, Exception inner) : base(message, inner) {}
		
		public VingdTransportException(string message, string context) : this(message, HttpStatusCode.Conflict, context) {}
		
		public VingdTransportException(string message, HttpStatusCode code, Exception inner) : base(message, inner) {
			this.code = code;
		}
		
		public VingdTransportException(string message, HttpStatusCode code, string context) : this(message, code, (Exception)null) {
			this.context = context;
		}

		public override string ToString() {
			return this.GetType().Name + ": " + (context != null ? "[" + context + "]: " : "") + Message + " (" + (int)code + " " + code.ToString() + ")";
		}
	}


	public class VingdClient {
        public const string userAgent = "vingd-api-csharp/1.1";
		
		// note: mono by default has empty trusted CA store
		//       Vingd uses DigiCert's certificate, so you should at least add their CA cert
		//       (see http://www.mono-project.com/FAQ:_Security on how to do it)

		// production/default Vingd endpoint and Vingd user frontend base
		public const string productionEndpointURL = "https://api.vingd.com/broker/v1";
		public const string productionFrontendURL = "https://www.vingd.com";
        
        // sandbox/testing Vingd endpoint and Vingd user frontend base
		public const string sandboxEndpointURL = "https://api.vingd.com/sandbox/broker/v1";
		public const string sandboxFrontendURL = "http://www.sandbox.vingd.com";

		// default order lifespan: 15min
		public TimeSpan defaultOrderExpiry = new TimeSpan(0, 15, 0);

		// default voucher lifespan: 1 month (i.e. 31 day)
		public TimeSpan defaultVoucherExpiry = new TimeSpan(31, 0, 0, 0);

		public enum ObjectClassID {
			Generic = 0
		}

		// connection parameters
		private string username = null;
		private string pwhash = null;
		private string backendURL = VingdClient.productionEndpointURL;
		private string frontendURL = VingdClient.productionFrontendURL;

		public VingdClient(string username, string pwhash, string backend, string frontend) {
			init(username, pwhash, backend, frontend);
		}

		public VingdClient(string username, string pwhash) {
			init(username, pwhash, VingdClient.productionEndpointURL, VingdClient.productionFrontendURL);
		}

		private void init(string username, string pwhash, string backendURL, string frontendURL) {
			this.username = username;
			this.pwhash = pwhash;
			this.backendURL = backendURL;
			this.frontendURL = frontendURL;
		}

		/**
		 * Helper methods
		 */
		public static string SHA1(string buffer) {
			byte[] hash = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(buffer));
			
			StringBuilder hex = new StringBuilder();
			for (int i = 0; i < hash.Length; i++) {
				hex.Append(hash[i].ToString("x2"));
			}
			return hex.ToString();
		}

		public object JSONParse (string json) {
			// hack: mono parser fix!
			json = Regex.Replace(json, @"null\s+}", "null}");
			json = Regex.Replace(json, @"true\s+}", "true}");
			json = Regex.Replace(json, @"false\s+}", "false}");
			JavaScriptSerializer jss = new JavaScriptSerializer();
			return jss.DeserializeObject(json);
		}

		public string JSONStringify (object obj) {
			JavaScriptSerializer jss = new JavaScriptSerializer();
			return jss.Serialize(obj);
		}

		/**
		 * Makes a generic Vingd Broker request.
		 * 
		 * Returns parsed JSON response in object; raises VingdTransportException or 
		 * VingdOperationException on failure.
		 * 
		 */
		public object Request (string method, string resource, object parameters) {
			UTF8Encoding encoder = new UTF8Encoding(false);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(backendURL + resource);
			req.Method = method;
			req.UserAgent = VingdClient.userAgent;
			
			// hack: seems like the standard Basic Http Auth in .NET sends auth header ONLY AFTER 
			// it receives the "401 Unauthenticated" response. PreAuthenticate is correcting this
			// on all (subsequent) requests, but the first one!
			// So, we have to fail back to the low-level method.
			
			//req.Credentials = new NetworkCredential(username, pwhash);
			//req.PreAuthenticate = true;
			string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + pwhash));
			req.Headers.Add("Authorization", "Basic " + credentials);
			
			if (parameters != null) {
				string strParameters = JSONStringify(parameters);
				byte[] byteParameters = encoder.GetBytes(strParameters);
				
				req.ContentType = "application/x-www-form-urlencoded";
				req.ContentLength = byteParameters.Length;
				
				Stream rs = req.GetRequestStream();
				rs.Write(byteParameters, 0, byteParameters.Length);
				rs.Close();
			}
			
			HttpWebResponse res;
			try {
				res = (HttpWebResponse)req.GetResponse();
			} catch (WebException e) {
				if (e.Status != WebExceptionStatus.ProtocolError || null == (res = e.Response as HttpWebResponse)) {
					throw new VingdTransportException("Connecting to Vingd Broker failed.", e);
				}
			}
			
			string jsonContent;
			try {
				StreamReader sr = new StreamReader(res.GetResponseStream(), encoder);
				jsonContent = sr.ReadToEnd();
			} catch (Exception e) {
				throw new VingdTransportException("Communication with Vingd Broker failed.", e);
			} finally {
				res.Close();
			}
			
			Dictionary<string, object> dictContent;
			try {
				dictContent = (Dictionary<string, object>)JSONParse(jsonContent);
			} catch (Exception e) {
				throw new VingdTransportException("Non-JSON response or unexpected JSON structure.", res.StatusCode, e);
			}
			
			// return data response if request successful
			int code = (int)res.StatusCode;
			if (code >= 200 && code < 300) {
				object data;
				try {
					data = (object)dictContent["data"];
				} catch (Exception e) {
					throw new VingdTransportException("Invalid JSON data response.", res.StatusCode, e);
				}
				return data;
			}
			
			// raise exception describing the vingd error condition
			string message, context;
			try {
				message = (string)dictContent["message"];
				context = (string)dictContent["context"];
			} catch (Exception e) {
				throw new VingdTransportException("Invalid JSON error response.", res.StatusCode, e);
			}
			throw new VingdOperationException(message, context, res.StatusCode);
		}

		private object GetDictListElem(Dictionary<string, object> dict, string key, int index) {
			if (!dict.ContainsKey(key)) return null;
			if (!dict[key].GetType().IsArray) return null;
			object[] array = (object[])dict[key];
			try {
				return array.GetValue(index);
			} catch (Exception) {
				return null;
			}
		}

		private Dictionary<string, object> ConvertToDict(object response) {
			if (response.GetType() != typeof(Dictionary<string, object>)) {
				throw new VingdTransportException("Unexpected service response; expecting JSON dictionary.", "Decode");
			}
			return (Dictionary<string, object>)response;
		}

		/**
		 * Registers (enrolls) an object into the Vingd Objects Registry.
		 * 
		 * For minimal object description ({'name': <name>, 'url': <callback_url>}), 
		 * use RegisterObject(string name, string url) method.
		 * 
		 */
		public long CreateObject (object description) {
			var parameters = new Dictionary<string, object>();
			parameters.Add("class", (int)ObjectClassID.Generic);
			parameters.Add("description", description);
			
			object responseRaw = Request("POST", "/registry/objects/", parameters);
			var response = ConvertToDict(responseRaw);
			
			var error = (Dictionary<string, object>)GetDictListElem(response, "errors", 0);
			if (error != null) {
				throw new VingdOperationException((string)error["desc"], (string)error["code"]);
			}
			
			object oid = GetDictListElem(response, "oids", 0);
			if (oid != null) {
				return Convert.ToInt64(oid);
			}
			
			throw new VingdTransportException("Unexpected service response.", "Decode");
		}

		public long CreateObject (string name, string url) {
			var description = new Dictionary<string, string>();
			description.Add("name", name);
			description.Add("url", url);
			return CreateObject((object)description);
		}

		/**
		 * Contacts Vingd Broker and generates a new order for selling the object (oid)
     	 * under the defined terms (price). Order shall be valid until expiresLocal.
     	 * Context shall be stored with order and returned upon purchase verification.
     	 * 
		 */
		public VingdOrder CreateOrder (long oid, double price, string context, DateTime expiresLocal) {
			DateTimeOffset expiresWithTimezone = new DateTimeOffset(expiresLocal, TimeZone.CurrentTimeZone.GetUtcOffset(expiresLocal));
			var parameters = new Dictionary<string, object>();
			parameters.Add("price", (int)(price * 100));
			parameters.Add("context", context);
			parameters.Add("order_expires", expiresWithTimezone.ToString("o"));
			
			object responseRaw = Request("POST", "/objects/" + oid + "/orders", parameters);
			var response = ConvertToDict(responseRaw);
			
			var error = (Dictionary<string, object>)GetDictListElem(response, "errors", 0);
			if (error != null) {
				throw new VingdOperationException((string)error["desc"], (string)error["code"]);
			}
			
			object id = GetDictListElem(response, "ids", 0);
			if (id == null) {
				throw new VingdTransportException("Unexpected service response.", "Decode");
			}
			long orderid = Convert.ToInt64(id);
			
			return new VingdOrder(orderid, oid, price, context, expiresWithTimezone, frontendURL);
		}

		public VingdOrder CreateOrder(long oid, double price, string context, TimeSpan expiry) {
			return CreateOrder(oid, price, context, DateTime.Now.Add(expiry));
		}
		
		public VingdOrder CreateOrder(long oid, double price, string context) {
			return CreateOrder(oid, price, context, DateTime.Now.Add(defaultOrderExpiry));
		}
		
		public VingdOrder CreateOrder(long oid, double price, TimeSpan expiry) {
			return CreateOrder(oid, price, null, DateTime.Now.Add(expiry));
		}

		public VingdOrder CreateOrder(long oid, double price) {
			return CreateOrder(oid, price, null, DateTime.Now.Add(defaultOrderExpiry));
		}

		/**
		 * Verifies the token of purchase thru Vingd Broker.
	     *
	     * If token was invalid (purchase can not be verified), VingdOperationException is thrown.
	     * 
		 */
		public VingdPurchase VerifyPurchase(string token) {
			string oid, tid;
			try {
				var tokenDict = (Dictionary<string, object>)JSONParse(token);
				oid = (string)tokenDict["oid"];
				tid = (string)tokenDict["tid"];
			} catch {
				throw new VingdTransportException("Invalid token.", "TokenVerification");
			}

			if (!IsTokenFormatValid(tid)) {
				throw new VingdTransportException("Invalid token format.", "TokenVerification");
			}

			object responseRaw = Request("GET", "/objects/" + oid + "/tokens/" + tid, null);
			var response = ConvertToDict(responseRaw);
			
			return new VingdPurchase {
				huid = (string)response["huid"],
				oid = Convert.ToInt64(oid),
				orderid = Convert.ToInt64(response["orderid"]),
				context = (string)response["context"],
				purchaseid = Convert.ToInt64(response["purchaseid"]),
				transferid = Convert.ToInt64(response["transferid"]),
				reclaimed = Convert.ToBoolean(response["reclaimed"])
			};
		}

		public bool IsTokenFormatValid (string tid) {
			return Regex.Match(tid, "^[a-fA-F\\d]{1,40}$").Success;
		}

		/**
		 * Commits user's reserved funds to seller account. Call CommitPurchase() upon
		 * successful delivery of paid content to the user. 
		 * 
		 * If you do not call CommitPurchase() user shall be automatically refunded.
		 * 
		 * On failure, VingdOperationException is thrown.
		 * 
		 */
		public void CommitPurchase(VingdPurchase purchase) {
			var parameters = new Dictionary<string, object>();
			parameters.Add("transferid", purchase.transferid);
			Request("PUT", "/purchases/" + purchase.purchaseid, parameters);
		}
		
		/**
		 * Creates a new Vingd voucher.
		 * 
		 * The voucher created vouches the `amountVouched` (in VINGDs) to the bearer 
		 * of the voucher (if used until `expiresLocal`). 
		 * Upon claiming the vouched vingds, user shall be presented with `userMessage`.
		 * 
		 * The key datum to present to the user is voucher claim URL on Vingd frontend,
		 * or alternatively the voucher code (returned .code / .GetRedirectURL()).
		 * 
		 */
		public VingdVoucher CreateVoucher(double amountVouched, DateTime expiresLocal, string userMessage) {
			DateTimeOffset expiresWithTimezone = new DateTimeOffset(expiresLocal, TimeZone.CurrentTimeZone.GetUtcOffset(expiresLocal));
			var parameters = new Dictionary<string, object>();
			parameters.Add("amount", (int)(amountVouched * 100));
			parameters.Add("until", expiresWithTimezone.ToString("o"));
			parameters.Add("message", userMessage);
			
			object responseRaw = Request("POST", "/vouchers/", parameters);
			var response = ConvertToDict(responseRaw);
			
			return new VingdVoucher(
				amountVouched,
				Convert.ToInt64(response["id_fort_transfer"]),
				expiresWithTimezone,
				(string)response["vid_encoded"],
				frontendURL
			);
		}
		
		public VingdVoucher CreateVoucher(double amountVouched, TimeSpan expiry, string userMessage) {
			return CreateVoucher(amountVouched, DateTime.Now.Add(expiry), userMessage);
		}

		public VingdVoucher CreateVoucher(double amountVouched, string userMessage) {
			return CreateVoucher(amountVouched, DateTime.Now.Add(defaultVoucherExpiry), userMessage);
		}
		
		public VingdVoucher CreateVoucher(double amountVouched) {
			return CreateVoucher(amountVouched, DateTime.Now.Add(defaultVoucherExpiry), null);
		}
		
		/**
		 * Rewards user identified with `huid`, directly with `amount` (in VINGDs).
		 * 
		 * Hashed User ID (huid) is bound to account of the authenticated user 
		 * (making the request). Transaction description can be set via `description` 
		 * parameter.
		 * 
		 * On failure, Vingd Exception is thrown.
		 * 
		 */
		public void RewardUser(string huid, double amount, string description) {
			var parameters = new Dictionary<string, object>();
			parameters.Add("huid_to", huid);
			parameters.Add("amount", (int)(amount * 100));
			parameters.Add("description", description);
			Request("POST", "/rewards/", parameters);
		}
		
		public void RewardUser(string huid, double amount) {
			RewardUser(huid, amount, null);
		}

	}

	
	public struct VingdOrder {
		public long id;
		public long oid;
		public double price;
		public string context;
		public DateTimeOffset expires;

		private string frontendURL;

		public VingdOrder(long id, long oid, double price, string context, DateTimeOffset expires, string frontendURL) {
			this.id = id;
			this.oid = oid;
			this.price = price;
			this.context = context;
			this.expires = expires;
			this.frontendURL = frontendURL;
		}

		public string GetRedirectURL() {
			return frontendURL + "/orders/" + id + "/add/";
		}
		
		public string GetPopupURL() {
			return frontendURL + "/popup/orders/" + id + "/add/";
		}
	}

	
	public struct VingdPurchase {
		public string huid;
		public long oid;
		public long orderid;
		public long purchaseid;
		public long transferid;
		public string context;
		public bool reclaimed;
	}
	
	
	public struct VingdVoucher {
		public double amount;
		public long transferid;
		public DateTimeOffset expires;
		public string code;
		
		private string frontendURL;
		
		public VingdVoucher(double amount, long transferid, DateTimeOffset expires, string code, string frontendURL) {
			this.amount = amount;
			this.transferid = transferid;
			this.expires = expires;
			this.code = code;
			this.frontendURL = frontendURL;
		}

		public string GetRedirectURL() {
			return frontendURL + "/vouchers/" + code;
		}
		
		public string GetPopupURL() {
			return frontendURL + "/popup/vouchers/" + code;
		}
	}
	
}
