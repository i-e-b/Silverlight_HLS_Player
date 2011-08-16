using System;
using System.Net;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace HCS.StreamSource {

	public class ChunkDetail {
		public string URL { get; set; }
		public TimeSpan ExpectedOffset { get; set; }
		public ChunkDetail (string url, TimeSpan offs) {
			URL = url;
			ExpectedOffset = offs;
		}
	}

	/// <summary>
	/// Handles keeping the playlist up-to-date
	/// </summary>
	public class PlaylistMgr: IDisposable {
		protected string PlaylistUrl;
		protected List<ChunkDetail> Chunks;
		protected DispatcherTimer timer;
		protected TimeSpan realDuration = TimeSpan.Zero; // the sum of fragment lengths, as measured by the encoder.
		protected int SeekChunkIndex; // starting index, set during 'Seek()'
		protected bool isLive, ready = true;
		protected WebClient wc;


		/// <summary>
		/// Create a new manager for the given playlist.
		/// Events will not fire until you call 'StartReading()'
		/// </summary>
		public PlaylistMgr (string PlaylistSourceUrl) {
			PlaylistUrl = PlaylistSourceUrl;
			timer = null;
			isLive = true; // the least dangerous setting.
			wc = new WebClient();
			wc.OpenReadCompleted += wc_OpenReadCompleted;
		}

		/// <summary>
		/// URL of selected playlist file
		/// </summary>
		public string PlaylistURL {
			get { return PlaylistUrl; }
		}

		/// <summary>
		/// Duration of a single chunk.
		/// </summary>
		public TimeSpan ChunkDuration { get; protected set; }

		/// <summary>
		/// Is the selected playlist file compressed?
		/// </summary>
		public bool IsCompressedPlaylist {
			get {
				if (String.IsNullOrEmpty(PlaylistUrl)) return false;
				return PlaylistUrl.ToUpper().EndsWith(".GZ");
			}
		}

		/// <summary>
		/// Number of chunks in the seek space
		/// </summary>
		public int AvailableChunkCount {
			get {
				return (Chunks.Count - 2);
			}
		}

		/// <summary>
		/// Highest value that can be passed to UrlOfOffset()
		/// </summary>
		public int MaximumIndex {
			get {
				return (Chunks.Count - 1) - SeekChunkIndex;
			}
		}

		/// <summary>
		/// Returns true if the playlist is being updated.
		/// Returns false if the playlist is completed (archive mode)
		/// </summary>
		/// <remarks>Normally</remarks>
		public bool IsLive {
			get { return isLive; }
		}

		/// <summary>
		/// Total duration so-far of this playlist (ignoring offsets and seek. This is the complete duration)
		/// </summary>
		public TimeSpan PlaylistDuration {
			get {
				if (realDuration > TimeSpan.Zero) return realDuration;
				// Otherwise, try to guess.
				if (Chunks == null) return TimeSpan.Zero;
				int safe_chunk_count = Chunks.Count - 2;
				if (!IsLive) safe_chunk_count += 1;
				if (safe_chunk_count < 0) safe_chunk_count = 0;
				return TimeSpan.FromSeconds(safe_chunk_count * ChunkDuration.TotalSeconds);
			}
		}

		/// <summary>
		/// Returns the chunk that contains the given play-head location.
		/// Inner seek routine, can also be use to match chunks to play-head times.
		/// </summary>
		public int GetChunkForTime (TimeSpan location) {
			if (Chunks == null || Chunks.Count < 2) return 0;

			TimeSpan now = Chunks[0].ExpectedOffset;
			TimeSpan next = Chunks[1].ExpectedOffset;
			int i = 0;

			if (location < TimeSpan.Zero) location = TimeSpan.Zero;

			int max = (IsLive) ? (Chunks.Count - 2) : (Chunks.Count - 1);

			while ((location > now) && (i < max - 1)) {
				if (next <= location) {
					i++;
					now = Chunks[i].ExpectedOffset;
					next = Chunks[i + 1].ExpectedOffset;
				} else {
					break;
				}
			}

			return i;
		}

		/// <summary>
		/// Seek the closest chunk to 'location'.
		/// Play will start from that chunk.
		/// Retuns real location set (between zero and 'PlaylistDuration')
		/// </summary>
		public TimeSpan Seek (TimeSpan location) {
			SeekChunkIndex = GetChunkForTime(location);
			OnSeekReset(null, null);
			return Chunks[SeekChunkIndex].ExpectedOffset;
		}

		/// <summary>
		/// Seek to a location near the end of the stream.
		/// </summary>
		public void SeekNearEnd () {
			SeekChunkIndex = Chunks.Count - 4; // three chunks back
			if (SeekChunkIndex < 0) SeekChunkIndex = 0;
			OnSeekReset(null, null);
		}

		/// <summary>
		/// Seek a difference from now (at chunk level).
		/// This isn't very accurate!
		/// </summary>
		internal void SeekOffset (TimeSpan offset) {
			var diff = (int)(offset.TotalSeconds / ChunkDuration.TotalSeconds);

			int pos = SeekChunkIndex + diff;
			if (pos < 0) pos = 0;
			int max = (IsLive) ? (Chunks.Count - 2) : (Chunks.Count - 1);
			if (pos > max) pos = max;

			SeekChunkIndex = pos;
			OnSeekReset(null, null);
		}

		/// <summary>
		/// Return the URL of the chunk at the given offset
		/// </summary>
		public string UrlOfOffset (int offset) {
			if (offset < 0 || offset > MaximumIndex) throw new ArgumentOutOfRangeException("offset", "offset must be between 0 and MaximumIndex");

			return Chunks[offset + SeekChunkIndex].URL;
		}

		/// <summary>
		/// Play time of offset. Use this for presentation times to MediaElement.
		/// </summary>
		/// <param name="offset">Chunk offset index</param>
		/// <returns>Presentation time</returns>
		public TimeSpan TimeOfOffset (int offset) {
			if (offset < 0 || offset > MaximumIndex) throw new ArgumentOutOfRangeException("offset", "offset must be between 0 and MaximumIndex");

			return Chunks[offset + SeekChunkIndex].ExpectedOffset;
		}

		public TimeSpan TimeOfIndex (int index) {
			if (index < 0) index = 0;
			if (index >= Chunks.Count) index = Chunks.Count - 1;

			return Chunks[index].ExpectedOffset;
		}

		public TimeSpan LastSeekTime {
			get {
				return TimeOfOffset(SeekChunkIndex);
			}
		}

		/// <summary>
		/// This event is fired every time the playlist is updated from it's source.
		/// The playlist has been parsed and is ready for action when this event is fired.
		/// </summary>
		public event EventHandler PlaylistUpdated;
		protected void OnPlaylistUpdated (object sender, EventArgs e) {
			if (PlaylistUpdated != null) {
				PlaylistUpdated(sender, e);
			}
		}

		/// <summary>
		/// This event is fired when the playlist is asked to seek.
		/// All responders should reset their offsets.
		/// </summary>
		public event EventHandler SeekReset;
		protected void OnSeekReset (object sender, EventArgs e) {
			if (SeekReset != null) {
				SeekReset(sender, e);
			}
		}

		/// <summary>
		/// This event is fired when the playlist is first retrieved and parsed.
		/// This event will only fire once, and only after the 'StartReading()' method is called
		/// </summary>
		public event EventHandler PlaylistReady;
		protected void OnPlaylistReady (object sender, EventArgs e) {
			if (PlaylistReady != null) {
				PlaylistReady(sender, e);
			}
		}

		/// <summary>
		/// Call this event once you've hooked up your event handlers to start firing events
		/// </summary>
		public void StartReading () {
			ReadPlaylist(null); // kick the reading & timer off.
		}

		protected void StartTimer () {
			timer = new DispatcherTimer{Interval = TimeSpan.FromTicks(ChunkDuration.Ticks * 2)};
			timer.Tick += ReadPlaylistAsync;
			timer.Start();
		}

		private void ReadPlaylistAsync (object source, EventArgs e) {
			if (wc.IsBusy) return; // already downloading
			if (!IsLive && timer != null) {
				try {
					timer.Stop();
				} catch {
					drop();
				}
			}
			System.Threading.ThreadPool.QueueUserWorkItem(ReadPlaylist);
		}

		private void drop() {}

		private void ReadPlaylist (object source) {
			if (String.IsNullOrEmpty(PlaylistUrl)) throw new Exception("Empty playlist url");

			if (wc.IsBusy) return; // already downloading

			wc.OpenReadAsync(new Uri(
				PlaylistUrl + "?" + DateTime.Now.Ticks.ToString("X"), // have to make sure the name is randomised, as WebClient has not cache invalidation
				UriKind.Absolute
				));
		}

		void wc_OpenReadCompleted (object sender, OpenReadCompletedEventArgs e) {
			if (e.Cancelled || e.Error != null) return;
			var new_chunks = new List<ChunkDetail>();

			lock (wc) {
				ParseHeaderFromWebRequest(e, new_chunks);
			}

			bool ShouldTriggerFirst = (Chunks == null);

			Chunks = new_chunks; // replace at end (as close to re-entrant as possible).

			if (ShouldTriggerFirst) OnPlaylistReady(null, null);

			OnPlaylistUpdated(null, null); // notify subscribers
		}

		private void ParseHeaderFromWebRequest (OpenReadCompletedEventArgs e, List<ChunkDetail> new_chunks) {
			StreamReader sr = null;
			using (var b_sr = new StreamReader(e.Result)) {
				try {
					if (IsCompressedPlaylist) {
						var x = new InflaterInputStream(b_sr.BaseStream);
						sr = new StreamReader(x);
					} else {
						sr = b_sr;
					}

					string base_url = PlaylistUrl.Substring(0, PlaylistUrl.LastIndexOf('/'));
					if (!base_url.EndsWith("/")) base_url += "/";

					string header = sr.ReadLine();
					if (header != "#EXTM3U") throw new Exception("Invalid playlist format: header");
					string[] duration = (sr.ReadLine()??"").Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
					double seconds;
					if ((duration.Length != 2)
						|| (duration[0] != "#EXT-X-TARGETDURATION")
						|| (!double.TryParse(duration[1], out seconds))
						) throw new Exception("Invalid playlist format: target duration");

					if (seconds < 1.0) throw new Exception("Invalid playlist: chunks too short (must be at least one second)");

					ChunkDuration = TimeSpan.FromSeconds(seconds);
					if (timer == null) StartTimer(); // we now have enough info to start the timer

					double total_seconds = 0.0; // sum of chunk durations, regardless of target time.

					do {
						string line = sr.ReadLine();

						if (line == null) break; // no more lines
						if (line.Length < 2) continue; // blank
						if (line.StartsWith("#EXTINF:")) { // this is the 'next video' marker.
							double secs = 0.0;
							try {// I'm using Apple's "title" part to give an accurate duration (which helps with seeking in very long videos)
								secs = double.Parse(line.Substring(line.IndexOf(',') + 1));
							} catch {drop(); }
							if (secs > 0.0 && secs < (seconds * 2)) total_seconds += secs; // if the encoder's figure seems reasonable
							else total_seconds += seconds; // add default
							continue;
						}
						if (line.StartsWith("#EXT-X-ENDLIST")) {
							//timer.Stop(); // this is a complete Playlist -- not live, so don't need periodic updates
							isLive = false;
							break;
						}
						if (line.StartsWith("#")) {
							// Some other extension line
							continue;
						}

						if (line.StartsWith("http://")) {
							new_chunks.Add(new ChunkDetail(line, TimeSpan.FromSeconds(total_seconds)));
						} else {
							new_chunks.Add(new ChunkDetail(base_url + line, TimeSpan.FromSeconds(total_seconds)));
						}
					} while (true);
				} finally {
					if (IsCompressedPlaylist && sr != null) {
						var x = sr.BaseStream as InflaterInputStream;
						if (x != null) {
							x.Close();
							x.Dispose();
						}
					}
				}
			}
		}

		/// <summary>
		/// Close timer and stop updating.
		/// </summary>
		public void Dispose () {
			if (timer != null) {
				timer.Stop();
			}
			if (wc != null && wc.IsBusy) {
				wc.CancelAsync();
			}
		}
/*
		/// <summary>
		/// Recommended time to buffer.
		/// </summary>
		public TimeSpan BufferTime () {
			double rec = Math.Max(15.0, ChunkDuration.TotalSeconds * 3.0);
			rec = Math.Min(30.0, rec);
			return TimeSpan.FromSeconds(rec);
		}
		*/
	}
}
