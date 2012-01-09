#pragma warning disable 169
using System;
using Machine.Specifications;

namespace PlaylistLoading {
	class When_reading_a_playlist : with.uri_to_a_single_bitrate_playlist {
		Because of = () => { result = null; };

		It should_do_something = () => result.ShouldNotBeNull();
	}

	#region contexts
	namespace with {
		[Subject("With a uri to a single bitrate playlist")]
		public abstract class uri_to_a_single_bitrate_playlist : /*Database*/ContextAndResult<Uri, PlaylistReader> {

			Establish context = () =>
			{
				subject = new Uri("");
			};
		}
	}
	#endregion
}

namespace PlaylistLoading.with {
	public class PlaylistReader {}
}

#pragma warning restore 169