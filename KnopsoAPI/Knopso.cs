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


namespace Knopso {
	
	public class KnopsoOperationException : Exception {
		protected string context = null;
		protected HttpStatusCode code = HttpStatusCode.Conflict;

		public KnopsoOperationException(string message, string context) : this(message, context, HttpStatusCode.Conflict, null) {}
		
		public KnopsoOperationException(string message, string context, HttpStatusCode code) : this(message, context, code, null) {}
		
		public KnopsoOperationException(string message, string context, HttpStatusCode code, Exception inner) 
		: base(message, inner) {
			this.context = context;
			this.code = code;
		}

		public override string ToString() {
			return this.GetType().Name + ": " + (context != null ? "[" + context + "]: " : "") + Message + " (" + (int)code + " " + code.ToString() + ")";
		}
	}

	
	public class KnopsoTransportException : Exception {
		protected string context = null;
		protected HttpStatusCode code = HttpStatusCode.Conflict;

		public KnopsoTransportException(string message, Exception inner) : base(message, inner) {}
		
		public KnopsoTransportException(string message, string context) : this(message, HttpStatusCode.Conflict, context) {}
		
		public KnopsoTransportException(string message, HttpStatusCode code, Exception inner) : base(message, inner) {
			this.code = code;
		}
		
		public KnopsoTransportException(string message, HttpStatusCode code, string context) : this(message, code, (Exception)null) {
			this.context = context;
		}

		public override string ToString() {
			return this.GetType().Name + ": " + (context != null ? "[" + context + "]: " : "") + Message + " (" + (int)code + " " + code.ToString() + ")";
		}
	}


	public class KnopsoBroker {
		// default knopso (server) backend and (user) frontend base urls
		public const string defaultBackendURL = "https://broker.knopso.com:8004";
		public const string defaultFrontendURL = "http://my.knopso.com";

		// default order lifespan: 15min
		public TimeSpan defaultOrderExpiry = new TimeSpan(0, 15, 0);

		public enum ObjectClassID {
			Generic = 0
		}

		// connection parameters
		private string username = null;
		private string pwhash = null;
		private string backendURL = KnopsoBroker.defaultBackendURL;
		private string frontendURL = KnopsoBroker.defaultFrontendURL;

		public KnopsoBroker(string username, string pwhash, string backend, string frontend) {
			init(username, pwhash, backend, frontend);
		}

		public KnopsoBroker(string username, string pwhash) {
			init(username, pwhash, KnopsoBroker.defaultBackendURL, KnopsoBroker.defaultFrontendURL);
		}

		private void init(string username, string pwhash, string backendURL, string frontendURL) {
			this.username = username;
			this.pwhash = pwhash;
			this.backendURL = backendURL;
			this.frontendURL = frontendURL;
			
			// hack: do not verify knopso backend self-signed cert
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
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
		 * Makes a generic Knopso Broker request.
		 * 
		 * Returns parsed JSON response in object; raises KnopsoTransportException or 
		 * KnopsoOperationException on failure.
		 * 
		 */
		public object Request (string method, string resource, object parameters) {
			UTF8Encoding encoder = new UTF8Encoding(false);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(backendURL + resource);
			req.Method = method;
			req.UserAgent = "KAPI/C#";
			
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
					throw new KnopsoTransportException("Connecting to Knopso Broker failed.", e);
				}
			}
			
			string jsonContent;
			try {
				StreamReader sr = new StreamReader(res.GetResponseStream(), encoder);
				jsonContent = sr.ReadToEnd();
			} catch (Exception e) {
				throw new KnopsoTransportException("Communication with Knopso Broker failed.", e);
			} finally {
				res.Close();
			}
			
			Dictionary<string, object> dictContent;
			try {
				dictContent = (Dictionary<string, object>)JSONParse(jsonContent);
			} catch (Exception e) {
				throw new KnopsoTransportException("Non-JSON response or unexpected JSON structure.", res.StatusCode, e);
			}
			
			// return data response if request successful
			int code = (int)res.StatusCode;
			if (code >= 200 && code < 300) {
				object data;
				try {
					data = (object)dictContent["data"];
				} catch (Exception e) {
					throw new KnopsoTransportException("Invalid JSON data response.", res.StatusCode, e);
				}
				return data;
			}
			
			// raise exception describing the knopso error condition
			string message, context;
			try {
				message = (string)dictContent["message"];
				context = (string)dictContent["context"];
			} catch (Exception e) {
				throw new KnopsoTransportException("Invalid JSON error response.", res.StatusCode, e);
			}
			throw new KnopsoOperationException(message, context, res.StatusCode);
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
				throw new KnopsoTransportException("Unexpected service response; expecting JSON dictionary.", "Decode");
			}
			return (Dictionary<string, object>)response;
		}

		/**
		 * Registers (enrolls) an object into the Knopso Registry.
		 * 
		 * For minimal object description ({'name': <name>, 'url': <callback_url>}), 
		 * use RegisterObject(string name, string url) method.
		 * 
		 */
		public long RegisterObject (object description) {
			var parameters = new Dictionary<string, object>();
			parameters.Add("class", (int)ObjectClassID.Generic);
			parameters.Add("description", description);
			
			object responseRaw = Request("POST", "/registry/objects/", parameters);
			var response = ConvertToDict(responseRaw);
			
			var error = (Dictionary<string, object>)GetDictListElem(response, "errors", 0);
			if (error != null) {
				throw new KnopsoOperationException((string)error["desc"], (string)error["code"]);
			}
			
			object oid = GetDictListElem(response, "oids", 0);
			if (oid != null) {
				return Convert.ToInt64(oid);
			}
			
			throw new KnopsoTransportException("Unexpected service response.", "Decode");
		}

		public long RegisterObject (string name, string url) {
			var description = new Dictionary<string, string>();
			description.Add("name", name);
			description.Add("url", url);
			return RegisterObject((object)description);
		}

		/**
		 * Contacts Knopso Broker and generates a new order for selling the object (oid)
     	 * under the defined terms (price). Order shall be valid until expiresLocal.
     	 * 
		 */
		public KnopsoOrder CreateOrder (long oid, double price, DateTime expiresLocal) {
			DateTimeOffset expiresWithTimezone = new DateTimeOffset(expiresLocal, TimeZone.CurrentTimeZone.GetUtcOffset(expiresLocal));
			var parameters = new Dictionary<string, object>();
			parameters.Add("price", (int)(price * 100));
			parameters.Add("order_expires", expiresWithTimezone.ToString("o"));
			
			object responseRaw = Request("POST", "/objects/" + oid + "/orders", parameters);
			var response = ConvertToDict(responseRaw);
			
			var error = (Dictionary<string, object>)GetDictListElem(response, "errors", 0);
			if (error != null) {
				throw new KnopsoOperationException((string)error["desc"], (string)error["code"]);
			}
			
			object id = GetDictListElem(response, "ids", 0);
			if (id == null) {
				throw new KnopsoTransportException("Unexpected service response.", "Decode");
			}
			long orderid = Convert.ToInt64(id);
			
			return new KnopsoOrder(orderid, oid, price, expiresWithTimezone, frontendURL);
		}

		public KnopsoOrder CreateOrder(long oid, double price, TimeSpan expiry) {
			return CreateOrder(oid, price, DateTime.Now.Add(expiry));
		}

		public KnopsoOrder CreateOrder(long oid, double price) {
			return CreateOrder(oid, price, DateTime.Now.Add(defaultOrderExpiry));
		}

		/**
		 * Verifies the token of purchase thru Knopso Broker.
	     *
	     * If token was invalid (purchase can not be verified), Knopso Exception is thrown.
	     * 
		 */
		public KnopsoPurchase VerifyToken(string token) {
			string oid, tid;
			try {
				var tokenDict = (Dictionary<string, object>)JSONParse(token);
				oid = (string)tokenDict["oid"];
				tid = (string)tokenDict["tid"];
			} catch {
				throw new KnopsoTransportException("Invalid token.", "TokenVerification");
			}
			
			object responseRaw = Request("GET", "/objects/" + oid + "/tokens/" + tid, null);
			var response = ConvertToDict(responseRaw);
			
			return new KnopsoPurchase {
				huid = (string)response["huid"],
				purchaseid = Convert.ToInt64(response["purchaseid"]),
				transferid = Convert.ToInt64(response["transferid"]),
				reclaimed = Convert.ToBoolean(response["reclaimed"])
			};
		}

		/**
		 * Commits user's reserved funds to seller account. Call CommitPurchase() upon
		 * successful delivery of paid content to the user. 
		 * 
		 * If you do not call CommitPurchase() user shall be automatically refunded.
		 * 
		 * On failure, Knopso Exception is thrown.
		 * 
		 */
		public void CommitPurchase(KnopsoPurchase purchase) {
			var parameters = new Dictionary<string, object>();
			parameters.Add("transferid", purchase.transferid);
			Request("PUT", "/purchases/" + purchase.purchaseid, parameters);
		}
		
	}

	
	public struct KnopsoOrder {
		public long id;
		public long oid;
		public double price;
		public DateTimeOffset expires;

		private string frontendURL;

		public KnopsoOrder(long id, long oid, double price, DateTimeOffset expires, string frontendURL) {
			this.id = id;
			this.oid = oid;
			this.price = price;
			this.expires = expires;
			this.frontendURL = frontendURL;
		}

		public string GetBuyURL() {
			return GetBuyURL(null);
		}
		public string GetBuyURL(string context) {
			if (context != null) context = "?context=" + Uri.EscapeUriString(context);
			return frontendURL + "/orders/" + id + "/add/" + context;
		}

		public string GetPopupBuyURL() {
			return GetPopupBuyURL(null);
		}
		public string GetPopupBuyURL(string context) {
			if (context != null) context = "?context=" + Uri.EscapeUriString(context);
			return frontendURL + "/popup/orders/" + id + "/add/" + context;
		}
	}

	
	public struct KnopsoPurchase {
		public string huid;
		public long purchaseid;
		public long transferid;
		public bool reclaimed;
	}
	
}
