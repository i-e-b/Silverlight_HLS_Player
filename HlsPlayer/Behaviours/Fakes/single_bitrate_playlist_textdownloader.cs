using System;
using HLSStreamSource.Playlists;

namespace Behaviours.Fakes {
	public class single_bitrate_playlist_textdownloader:ITextDownloader {
		Uri last_target;

		public int NumberOfFragmentsSupplied {
			get { return 15; }
		}

		public void Fetch(Uri target, Action<string> Success, Action<Exception> Failure) {
			last_target = target;
			Success(@"#EXTM3U
#EXT-X-TARGETDURATION:10
#EXT-X-MEDIA-SEQUENCE:0
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_0.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_1.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_2.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_3.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_4.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_5.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_6.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_7.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_8.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_9.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_10.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_11.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_12.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_13.ts
#EXTINF:10,
http://ispss.istreamplanet.com/iphone/robinhood_wifi/Seg_040310_153539_0/robinhood_wifi_040310_153539_14.ts
#EXT-X-ENDLIST 
");
		}

		public bool WasCalledWith(Uri target) {
			return target == last_target;
		}
	}
}
