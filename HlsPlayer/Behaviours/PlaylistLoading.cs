#pragma warning disable 169
using System;
using Machine.Specifications;
using MediaStreamSource.Playlists;
using Moq;
using It = Machine.Specifications.It;

namespace PlaylistLoading {
	class When_reading_a_playlist : with.uri_to_a_single_bitrate_playlist {
		Because of = () => { result = null; };

		It should_do_something = () => result.ShouldNotBeNull();
	}

	#region contexts
	namespace with {
		[Subject("With a uri to a single bitrate playlist")]
		public abstract class uri_to_a_single_bitrate_playlist : ContextAndResult<Uri, PlaylistReader> {
			public static Mock<ITextDownloader> downloader;

			Establish context = () =>
			{
				downloader = new Mock<ITextDownloader>();
				subject = new Uri("http://www.example.com/playlist.m3u8");
				downloader.Setup(d=>d.Fetch(subject, It.IsAny<Action<string>>(),
			};
		}
	}
	#endregion
}

namespace PlaylistLoading.with {
	public class PlaylistReader {}
}

#pragma warning restore 169