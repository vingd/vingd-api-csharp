using System;
using System.Web;
using System.Web.UI;
using System.Collections;
using System.Collections.Generic;
using Vingd;

namespace VideoMonetizationWebExample {
	public partial class Video : System.Web.UI.Page {
		// mock db (local video id <-> vimeo video id)
		private Dictionary<int, string> dbVideos = new Dictionary<int, string>() {
			{1, "36296951"},
			{2, "7174318"}
		};
		private string tplVideoLink = "http://player.vimeo.com/video/{0}?autoplay=1";
		
		private VingdClient vingd;
		public Video() {
			vingd = new VingdHandler().GetVingdClient();
		}
		
		private string GetVideoLink(int vid) {
			if (!dbVideos.ContainsKey(vid)) 
				throw new Exception("Unknown Video ID!");
			return String.Format(tplVideoLink, dbVideos[vid]);
		}
		
		public string GetVideo() {
			int vid = Convert.ToInt32(Request["vid"]);
			string token = Request["token"];
			VingdPurchase purchase;
			try {
				purchase = vingd.VerifyPurchase(token);
			} catch (VingdOperationException e) {
				return e.Message;
			} catch (Exception e) {
				return e.Message;
			}
			
			string html;
			try {
				html = String.Format("<iframe src=\"{0}\" width=\"500\" height=\"281\" frameborder=\"0\" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>", GetVideoLink(vid));
				vingd.CommitPurchase(purchase);
			} catch (Exception e) {
				return e.Message;
			}
			
			return html;
		}
	}
}
