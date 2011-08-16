using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace HCS.StreamSource {
	public class HCS_MediaStreamingSource : MediaStreamSource, IDisposable{
		#region Junk
		protected MediaStreamDescription videoStreamDescription;
		protected MediaStreamDescription audioStreamDescription;
		protected PlaylistMgr playlist;
		protected DispatcherTimer timer;

		protected mpegts_demux_buffer DemuxBuffer;

		/// <summary>
		/// Gets the MpegLayer3WaveFormat structure which represents an Mp3 file.
		/// </summary>
		protected MpegLayer3WaveFormat MpegLayer3WaveFormat { get; set; }
		#endregion

		/// <summary>
		/// Start a new streaming source with a prepared playlist.
		/// This should be called in response to the Playlist's 'PlaylistReady' event.
		/// </summary>
		public HCS_MediaStreamingSource (PlaylistMgr Playlist) {
			playlist = Playlist;
			DemuxBuffer = new mpegts_demux_buffer(Playlist);

			DemuxBuffer.AudioSamplesAvailable += new EventHandler(DemuxBuffer_AudioSamplesAvailable);
			DemuxBuffer.VideoSamplesAvailable += new EventHandler(DemuxBuffer_VideoSamplesAvailable);

			StartTimer();
		}

		protected void StartTimer () {
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(0.5) ;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}

		void timer_Tick (object sender, EventArgs e) {
			DemuxBuffer.Pump();
		}

		public void Dispose () {
			if (timer != null) timer.Stop();
			DemuxBuffer.Dispose();
		}


		#region Sample transport (fetch from Demux buffer)

		void DemuxBuffer_VideoSamplesAvailable (object sender, EventArgs e) {
			GenericMediaFrame frame = null;

			lock (DemuxBuffer.VideoFrames) {
				if (DemuxBuffer.VideoFrames.Count > 0) {
					while (frame == null) {
						frame = DemuxBuffer.VideoFrames.Dequeue();
					}
				} else {
					ReportEndOfMedia(MediaStreamType.Video);
					return;
				}
			}

			TimeSpan when = TimeSpan.FromTicks((long)frame.FramePresentationTime);

			var flags = new Dictionary<MediaSampleAttributeKeys, string>();
			MemoryStream ms = new MemoryStream(frame.FrameData);

			try {
				MediaStreamSample samp = new MediaStreamSample(
						this.videoStreamDescription,
						ms, 0, frame.FrameData.Length,
						frame.FramePresentationTime,
						flags);

				ReportGetSampleProgress(1.0);
				ReportGetSampleCompleted(samp);
			} catch { }
		}

		void DemuxBuffer_AudioSamplesAvailable (object sender, EventArgs e) {
			GenericMediaFrame frame = null;

			lock (DemuxBuffer.AudioFrames) {
				if (DemuxBuffer.AudioFrames.Count > 0) {
					while (frame == null) {
						frame = DemuxBuffer.AudioFrames.Dequeue();
					}
				} else {
					ReportEndOfMedia(MediaStreamType.Audio);
					return;
				}
			}

			var flags = new Dictionary<MediaSampleAttributeKeys, string>();
			MemoryStream ms = new MemoryStream(frame.FrameData);
			try {
				MediaStreamSample samp = new MediaStreamSample(
						this.audioStreamDescription,
						ms, 0, frame.FrameData.Length,
						frame.FramePresentationTime,
						flags);

				ReportGetSampleProgress(1.0);
				ReportGetSampleCompleted(samp);
			} catch { }
		}

		#endregion

		#region Responders
		protected override void CloseMedia () {
			// Nothing yet
		}

		protected override void GetDiagnosticAsync (MediaStreamSourceDiagnosticKind diagnosticKind) {
			ReportGetDiagnosticCompleted(diagnosticKind, 0);
		}

		protected override void GetSampleAsync (MediaStreamType mediaStreamType) {
			// Call the playlist to cue the next chunk, then exit and wait for the available event.
			switch (mediaStreamType) {
				case MediaStreamType.Audio:
					DemuxBuffer.RequestAudioSample();
					break;

				case MediaStreamType.Video:
					DemuxBuffer.RequestVideoSample();
					break;
			}
		}

		private void ReportEndOfMedia (MediaStreamType mediaStreamType) {
			var flags = new Dictionary<MediaSampleAttributeKeys, string>();

			MediaStreamDescription msd = this.videoStreamDescription;
			if (mediaStreamType == MediaStreamType.Audio) msd = this.audioStreamDescription;
			try {
				MediaStreamSample samp = new MediaStreamSample(msd, null, 0, 0, 0, flags);
				ReportGetSampleCompleted(samp);
			} catch { }
		}

		protected override void SeekAsync (long seekToTime) {
			ReportSeekCompleted(0); // must always return this, or MediaElement will go nuts.
		}
		protected override void SwitchMediaStreamAsync (MediaStreamDescription mediaStreamDescription) {
			// nothing.
		}
		#endregion

		#region Setup & Configuration

		/// <summary>
		/// Kick-start the media player.
		/// Adds H264 playing info.
		/// </summary>
		protected override void OpenMediaAsync () {
			Dictionary<MediaSourceAttributesKeys, string> mediaSourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
			Dictionary<MediaStreamAttributeKeys, string> videoStreamAttributes = GetVideoSettings();
			Dictionary<MediaStreamAttributeKeys, string> audioStreamAttributes = GetAudioSettings();

			List<MediaStreamDescription> mediaStreamDescriptions = new List<MediaStreamDescription>();

			this.videoStreamDescription = new MediaStreamDescription(MediaStreamType.Video, videoStreamAttributes);
			mediaStreamDescriptions.Add(this.videoStreamDescription);
			
			this.audioStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, audioStreamAttributes);
			mediaStreamDescriptions.Add(this.audioStreamDescription);

			mediaSourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.MaxValue.Ticks.ToString(CultureInfo.InvariantCulture);
			mediaSourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

			this.ReportOpenMediaCompleted(mediaSourceAttributes, mediaStreamDescriptions);
		}

		/// <summary>
		/// Build description for Video stream.
		/// TODO: get Codec data for framerate
		/// </summary>
		protected Dictionary<MediaStreamAttributeKeys, string> GetVideoSettings () {
			Dictionary<MediaStreamAttributeKeys, string> mediaStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
			mediaStreamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "H264";

			// 'iPhone' video is native at 480x320.
			mediaStreamAttributes[MediaStreamAttributeKeys.Width] = "480";
			mediaStreamAttributes[MediaStreamAttributeKeys.Height] = "320";

			return mediaStreamAttributes;
		}

		/// <summary>
		/// Build description for Audio stream.
		/// </summary>
		protected Dictionary<MediaStreamAttributeKeys, string> GetAudioSettings () {
			Dictionary<MediaStreamAttributeKeys, string> mediaStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();

			// Initialize the Mp3 data structures used by the Media pipeline with state from the first frame.
			// These are all defaults for the HCS system. reading these in properly is a total ass-pain.
			WaveFormatExtensible wfx = new WaveFormatExtensible();
			this.MpegLayer3WaveFormat = new MpegLayer3WaveFormat();
			this.MpegLayer3WaveFormat.WaveFormatExtensible = wfx;

			this.MpegLayer3WaveFormat.WaveFormatExtensible.FormatTag = 85;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.Channels = 1;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.SamplesPerSec = 44100;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.AverageBytesPerSecond = 96000 / 8;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.BlockAlign = 1;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.BitsPerSample = 0;
			this.MpegLayer3WaveFormat.WaveFormatExtensible.Size = 12;

			this.MpegLayer3WaveFormat.Id = 1;
			this.MpegLayer3WaveFormat.BitratePaddingMode = 0;
			this.MpegLayer3WaveFormat.FramesPerBlock = 1;
			this.MpegLayer3WaveFormat.BlockSize = 312;
			this.MpegLayer3WaveFormat.CodecDelay = 0;

			mediaStreamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = this.MpegLayer3WaveFormat.ToHexString();

			return mediaStreamAttributes;
		}

		#endregion

	}
}
