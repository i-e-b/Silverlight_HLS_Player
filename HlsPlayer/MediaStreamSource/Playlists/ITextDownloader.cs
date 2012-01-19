using System;

namespace HLSStreamSource.Playlists {
	public interface ITextDownloader {
		void Fetch (Uri target, Action<string> Success, Action<Exception> Failure);
	}
}
