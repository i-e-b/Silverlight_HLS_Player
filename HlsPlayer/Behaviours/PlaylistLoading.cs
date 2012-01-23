#pragma warning disable 169
using System;
using System.Collections.Generic;
using Behaviours.Fakes;
using HLSStreamSource.Playlists;
using Machine.Specifications;
using It = Machine.Specifications.It;
using System.Linq;

namespace PlaylistLoading {
	class When_reading_a_playlist : with.uri_to_a_single_bitrate_playlist {
		Because of = () => subject.Read(target);

		It should_call_downloader_to_get_playlist = () => downloader.WasCalledWith(target).ShouldBeTrue();
		It should_have_a_single_bitrate_available = () => subject.Bitrates.Count().ShouldEqual(1);

		It should_have_a_list_of_fragments_in_the_playlist = () =>
			subject[subject.Bitrates[0]].Count().ShouldEqual(downloader.NumberOfFragmentsSupplied);
	}

	

	#region contexts
	namespace with {
		[Subject("with a uri to a single bitrate playlist")]
		public abstract class uri_to_a_single_bitrate_playlist : ContextOf<PlaylistReader> {
			public static playlist_textdownloader downloader;
			public static Uri target;

			Establish context = () =>
			{
				downloader = new playlist_textdownloader();
				target = downloader.WifiUri;
				subject = new PlaylistReader(downloader);
			};
		}
	}
	#endregion
}

namespace PlaylistLoading {
	public class Fragment {
		public string Url { get; private set; }
		public Fragment(string url) { Url = url; }
	}
	public class PlaylistReader {
		readonly ITextDownloader textDownloader;
		readonly Dictionary<int, List<Fragment>> bitrateFragments;

		public PlaylistReader (ITextDownloader textDownloader) {
			this.textDownloader = textDownloader;
			bitrateFragments = new Dictionary<int, List<Fragment>>();
		}

		public int[] Bitrates { get { return bitrateFragments.Keys.ToArray(); }
		}

		public void Read (Uri subject) {
			textDownloader.Fetch(subject, ParsePlaylist, HandleError);
		}

		void HandleError(Exception obj) {  }

		void ParsePlaylist(string obj) {
			bitrateFragments.Add(0, new List<Fragment>());
			var frags = bitrateFragments[0];

			var lines = obj.Split('\r', '\n');
			if (lines[0] != "#EXTM3U") throw new Exception("not a valid playlist");

			// todo: check if this is a multibitrate playlist 

			frags.AddRange(from line in lines
			               where !line.StartsWith("#")
			               where !string.IsNullOrWhiteSpace(line)
			               select new Fragment(line));
		}

		public IEnumerable<Fragment> this[int i] {
			get { return bitrateFragments[i]; }
		}
	}
}

#pragma warning restore 169