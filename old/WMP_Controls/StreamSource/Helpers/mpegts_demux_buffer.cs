using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;


namespace HCS.StreamSource {

	/// <summary>
	/// Class to handle the input of MPEG Transport files.
	/// Keeps a buffer of the Video and Audio streams.
	/// Can be used to pump a MediaStreamingSource, and will keep it's own buffer.
	/// </summary>
	public class mpegts_demux_buffer : IDisposable {
		#region Properties & Members
		public Queue<GenericMediaFrame> VideoFrames {
			get;
			private set;
		}
		public Queue<GenericMediaFrame> AudioFrames {
			get;
			private set;
		}

		protected int WaitingVideoSamples = 0;
		protected int WaitingAudioSamples = 0;
		protected WebClient wc;
		protected PlaylistMgr playlist;
		protected int chunkOffset = 0;
		protected bool finished = false; // true when the stream is not live and has no more fragments
		protected bool ready = true; // used to prevent pumping after disposal.
		private bool busy = false; // to synchronise the non-thread-safe async WebClient...

		protected MpegTS_Demux demux;
		#endregion

		#region Events

		/// <summary>
		/// This event called after an audio request once some samples are ready
		/// </summary>
		public event EventHandler AudioSamplesAvailable;
		protected void OnAudioSamplesAvailable (object sender, EventArgs e) {
			if (ready && AudioSamplesAvailable != null) {
				AudioSamplesAvailable(sender, e);
			}
		}

		/// <summary>
		/// This event called after an video request once some samples are ready
		/// </summary>
		public event EventHandler VideoSamplesAvailable;
		protected void OnVideoSamplesAvailable (object sender, EventArgs e) {
			if (ready && VideoSamplesAvailable != null) {
				VideoSamplesAvailable(sender, e);
			}
		}

		#endregion


		/// <summary>
		/// Create a new empty demuxer-buffer
		/// </summary>
		public mpegts_demux_buffer (PlaylistMgr Playlist) {
			demux = new MpegTS_Demux();
			VideoFrames = new Queue<GenericMediaFrame>();
			AudioFrames = new Queue<GenericMediaFrame>();
			playlist = Playlist;
			wc = new WebClient();
			wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
		}

		/// <summary>
		/// Rip a transport file into it's constituent elementary streams.
		/// Currently only handles transports with 1 audio and 1 video.
		/// </summary>
		/// <param name="MinimumTimecode">Time code in MPEG ticks (90kHz). If PTS is less than this, it is increased by the 32bit limit until over.</param>
		protected void FeedTransportStream (Stream TransportStream, ulong MinimumTimecode) {
			demux.FeedTransportStream(TransportStream, MinimumTimecode);
			foreach (var vfr in demux.GetAvailableVideo()) {
				VideoFrames.Enqueue(vfr);
			}

			foreach (var afr in demux.GetAvailableAudio()) {
				AudioFrames.Enqueue(afr);
			}
			busy = false;
			Pump();
		}

		/// <summary>
		/// Output samples and grab more if needed.
		/// Media stream source should call this periodically
		/// </summary>
		public void Pump () {
			if (!ready) return;
			while (VideoFrames.Count > 0 && WaitingVideoSamples > 0) {
				OnVideoSamplesAvailable(null, null);
				WaitingVideoSamples--;
			}

			while (AudioFrames.Count > 0 && WaitingAudioSamples > 0) {
				OnAudioSamplesAvailable(null, null);
				WaitingAudioSamples--;
			}

			if ((WaitingAudioSamples > 0)
				||
				(WaitingVideoSamples > 0)
				||
				(VideoFrames.Count < 450) // 3 chunks of 5 seconds at 30fps.
				||
				(AudioFrames.Count < 50) // video frames should catch everything, but this is a safeguard
				) FetchNewFragments();
		}

		/// <summary>
		/// Get the next fragment file from the intertubes.
		/// </summary>
		protected void FetchNewFragments () {
			if (!ready) return;
			if (wc.IsBusy || busy) return; // The tubes are blocked
			if (chunkOffset <= playlist.MaximumIndex) {
				// OK to start fetching.
				string url = playlist.UrlOfOffset(chunkOffset);
				try {
					wc.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
					chunkOffset++; // NEXT!
				} catch { } // Sodding lousy WebClient!!!
			} else {
				if (playlist.IsLive) return; // no fragments available, but there probably will be later
				else finished = true; // totally out of fragments.
			}
		}


		/// <summary>
		/// Called when a media chunk is downloaded
		/// </summary>
		void wc_OpenReadCompleted (object sender, OpenReadCompletedEventArgs e) {
			if (e.Cancelled || e.Error != null) {
				//chunkOffset--; // we need this one again!
				// TODO: protect against total mash if on fragment is missing
				return;
			}

			busy = true; // not now Bernard!
			// WebClient puts us back on the UI thread, so we need to drop out again...
			System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(FeedBufferAction), e);
		}

		private void FeedBufferAction (object data) {
			OpenReadCompletedEventArgs e = data as OpenReadCompletedEventArgs;
			long timestamp = Math.Max(0L, playlist.TimeOfOffset(chunkOffset - 1).Subtract(TimeSpan.FromHours(1)).Ticks);
			try {
				FeedTransportStream(e.Result, (ulong)timestamp); // decode, then put in the buffers
			} catch { } // Hide some seek/disposal problems
		}

		/// <summary>
		/// Try to call back 'VideoSamplesAvailable' as soon as possible
		/// </summary>
		internal void RequestVideoSample () {
			if (!ready) return; // we're being closed down!
			if (VideoFrames.Count > 0 || finished) {
				OnVideoSamplesAvailable(null, null);
			} else {
				WaitingVideoSamples++;
				FetchNewFragments();
			}
		}

		/// <summary>
		/// Try to call back 'AudioSamplesAvailable' as soon as possible
		/// </summary>
		internal void RequestAudioSample () {
			if (!ready) return; // going out of business!
			if (AudioFrames.Count > 0 || finished) {
				OnAudioSamplesAvailable(null, null);
			} else {
				WaitingAudioSamples++;
				FetchNewFragments();
			}
		}

		#region IDisposable Members

		public void Dispose () {
			ready = false; // stop sending events
			busy = true; // don't try to start any new downloads!
			if (wc != null && wc.IsBusy) wc.CancelAsync(); // stop ongoing downloads
			VideoFrames.Clear();
			VideoFrames = null;
			AudioFrames.Clear();
			AudioFrames = null;

			GC.Collect();
		}

		#endregion
	}
}
