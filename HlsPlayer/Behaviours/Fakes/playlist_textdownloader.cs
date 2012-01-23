using System;
using HLSStreamSource.Playlists;

namespace Behaviours.Fakes {
	public class playlist_textdownloader:ITextDownloader {
		Uri last_target;

		public int NumberOfFragmentsSupplied {
			get { return 15; }
		}

		public int NumberOfBitratesSupplied {
			get { return 4; }
		}

		public Uri WifiUri {
			get { return new Uri("http://ispss.istreamplanet.com/iphone/robinhood_wifi.m3u8", UriKind.Absolute); }
		}

		public void Fetch(Uri target, Action<string> Success, Action<Exception> Failure) {
			last_target = target;
			Success(ContentForUri(target));
		}

		public bool WasCalledWith(Uri target) {
			return target == last_target;
		}

		string ContentForUri (Uri target) {
			switch (target.AbsoluteUri) {
				case "http://ispss.istreamplanet.com/iphone/robinhood_Edge.m3u8":
				case "http://ispss.istreamplanet.com/iphone/robinhood_3GLow.m3u8":
				case "http://ispss.istreamplanet.com/iphone/robinhood_3Ghi.m3u8":
				case "http://ispss.istreamplanet.com/iphone/robinhood_wifi.m3u8":
					return @"#EXTM3U
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
";

				default:
					return @"#EXTM3U
#EXT-X-STREAM-INF:PROGRAM-ID=1, BANDWIDTH=181674
http://ispss.istreamplanet.com/iphone/robinhood_Edge.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1, BANDWIDTH=448577
http://ispss.istreamplanet.com/iphone/robinhood_3GLow.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1, BANDWIDTH=798577
http://ispss.istreamplanet.com/iphone/robinhood_3Ghi.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1, BANDWIDTH=1598577
http://ispss.istreamplanet.com/iphone/robinhood_wifi.m3u8
";
			}
		}
	}
}
