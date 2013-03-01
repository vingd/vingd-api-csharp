using System;
using System.Web;
using System.Web.UI;
using Vingd;

namespace VideoMonetizationWebExample {
	public partial class Default : System.Web.UI.Page {
		public const string baseURL = "http://127.0.0.1:8080";
		public const string videoBaseURL = baseURL + "/Video.aspx";
		public const string vingdFrontendURL = VingdHandler.vingdFrontendURL;
		
		public const string vingdVideoName = "Demo Video";
		public const string vingdVideoURL = videoBaseURL;
		public const double vingdVideoPrice = 2.00;
		public TimeSpan vingdVideoOrderExpiry = new TimeSpan(365, 0, 0, 0);
		
		public VingdHandler v;
		
		public Default() {
			v = new VingdHandler(vingdVideoName, vingdVideoURL, vingdVideoPrice, vingdVideoOrderExpiry);
		}
	}
}
