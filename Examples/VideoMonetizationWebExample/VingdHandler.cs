using System;
using Vingd;
using System.Collections;
using System.Collections.Generic;

namespace VideoMonetizationWebExample {
	public class VingdHandler {
		public const string vingdEndpointURL = VingdClient.sandboxEndpointURL;
		public const string vingdFrontendURL = VingdClient.sandboxFrontendURL;
		private const string vingdUsername = "test@vingd.com";
		private const string vingdPassword = "123";
		private VingdClient vingd = null;
		
		private string objectName = null;
		private string objectURL = null;
		private double orderPrice = 0;
		private TimeSpan orderExpiry;
		
		private long objectID = 0;
		private Dictionary<int, VingdOrder> videoOrders = new Dictionary<int, VingdOrder>();
		
		public VingdHandler() {
			string vingdPasswordHash = VingdClient.SHA1(vingdPassword);
			
			// during development and testing, use Vingd sandbox:
			vingd = new VingdClient(vingdUsername, vingdPasswordHash, vingdEndpointURL, vingdFrontendURL);
			
			// in production, use:
			//vingd = new VingdClient(vingdUsername, vingdPasswordHash);
		}
		
		public VingdHandler(string objectName, string objectURL, double price, TimeSpan expiry) : this() {
			this.objectName = objectName;
			this.objectURL = objectURL;
			this.orderPrice = price;
			this.orderExpiry = expiry;
		}

		// TODO: persistent storage
		// TEMP: just for a demo, we'll always create a new Vingd Object (per request)
		public long GetVideoObjectID() {
			if (objectID == 0)
				objectID = vingd.CreateObject(objectName, objectURL);
			return objectID;
		}
		
		// TODO: persistent orders storage
		// TEMP: just for a demo, we'll create a new Vingd Order pre video (per request)
		public VingdOrder GetVideoOrder(int videoID) {
			if (!videoOrders.ContainsKey(videoID)) {
				videoOrders.Add(videoID, vingd.CreateOrder(GetVideoObjectID(), orderPrice, orderExpiry));
			}
			return videoOrders[videoID];
		}
		
		public VingdClient GetVingdClient() {
			return vingd;
		}
	}
}
