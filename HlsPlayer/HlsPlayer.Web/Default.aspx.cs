using System;
using System.Web.UI;

namespace SilverlightPlayer.Web {
	public partial class _Default : Page {

		public string Playlist2 {
			get {
				return "need another demo...";
			}
		}

		public string Playlist1 {
			get {
				/*if (string.IsNullOrEmpty(Request["plst"])) */
					return "http://streaming.smartmove.pt/smooths/hls/hd/elephants.ssm/elephants-186k.m3u8";//elephants.m3u8";
				//return "http://" + Request["plst"];
			}
		}

		protected void Page_Load (object sender, EventArgs e) {
			if (!(Request.UserAgent ?? "").ToUpper().Contains("IPHONE")) return;
			Response.Redirect("iphone.aspx");
			Response.End();
			return;
		}
	}
}
