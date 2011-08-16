using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using HCS.StreamSource;

namespace WMP_Controls {
	public partial class MainPage : UserControl {

		private bool NeedToReset = false;
		private bool TimerWorking = false;
		private bool Ready = false;
		private TimeSpan NextSeekLocation = TimeSpan.Zero;
		private int? MaybeStartTime = null;
		protected PlaylistMgr Playlist;
		protected HCS_MediaStreamingSource PlayerStreamSource;
		private System.Windows.Threading.DispatcherTimer timer = null;

		public string MediaURL {
			get {
				return Playlist.PlaylistURL;
			}
			set {
				SetPlaylist(value);
			}
		}

		/// <summary>
		/// Prepare and start a new playlist, hooked up to the ready event.
		/// </summary>
		private void SetPlaylist (string value) {
			Playlist = new PlaylistMgr(value /*"http://www.arqiva.twofourstaging.net/hls/index.m3u8.gz"*/);

			Playlist.PlaylistReady += new EventHandler(Playlist_PlaylistReady);
			Playlist.StartReading();
		}
		/// <summary>
		/// Playlist is ready -- hook up the MediaStreamingSource and start!
		/// </summary>
		void Playlist_PlaylistReady (object sender, EventArgs e) {
			MediaPlayer.Stop();
			MediaPlayer.AutoPlay = true;

			if (MaybeStartTime.HasValue) {
				Playlist.Seek(TimeSpan.FromSeconds(MaybeStartTime.Value));
			} else if (Playlist.IsLive) {
				Playlist.SeekNearEnd();
			} // otherwise will be going from start.
			if (PlayerStreamSource != null) PlayerStreamSource.Dispose();
			PlayerStreamSource = new HCS_MediaStreamingSource(Playlist);

			Ready = true;
			MediaPlayer.SetSource(PlayerStreamSource);
		}

		public string PositionString {
			get { return Position.ToString(); }
			set { Position = int.Parse(value); }
		}

		public int Position {
			get {
				return (int)(CurrentPosition.TotalSeconds);
			}
			set {
				if (value <= 0) return;
				if (Ready) {
					Seek(TimeSpan.FromSeconds(value));
				} else {
					MaybeStartTime = value;
				}
			}
		}

		public void Play () {
			MediaPlayer.Play();
		}
		public void Stop () {
			MediaPlayer.Pause();
		}
		public void Pause () {
			MediaPlayer.Pause();
		}


		public MainPage () {
			// Required to initialize variables
			InitializeComponent();

			//ScalePlayer();
			this.SizeChanged += new SizeChangedEventHandler(MainPage_SizeChanged);
			this.LayoutUpdated += new EventHandler(MainPage_LayoutUpdated);

			MediaControls.TogglePlay += new EventHandler(MediaControls_TogglePlay);
			MediaControls.StopVideo += new EventHandler(MediaControls_StopVideo);
			MediaControls.JumpBack += new EventHandler(MediaControls_JumpBack);
			MediaControls.JumpForward += new EventHandler(MediaControls_JumpForward);
			MediaControls.VolumeChanged += new EventHandler(MediaControls_VolumeChanged);
			MediaControls.PositionMarkerChanged+=new EventHandler<WMP_Player_Controls.TimeSpanEvent>(MediaControls_PositionMarkerChanged);
			MediaControls.FastForward += new EventHandler(MediaControls_FastForward);
			MediaControls.Rewind += new EventHandler(MediaControls_Rewind);
			MediaControls.GoFullscreen += new EventHandler(MediaControls_GoFullscreen);

			MediaPlayer.CurrentStateChanged += new RoutedEventHandler(MediaPlayer_CurrentStateChanged);
			MediaPlayer.MarkerReached += new TimelineMarkerRoutedEventHandler(MediaPlayer_MarkerReached);
			MediaPlayer.MediaEnded += new RoutedEventHandler(MediaPlayer_MediaEnded);
			MediaPlayer.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(MediaPlayer_MediaFailed);

			this.SizeChanged+=new SizeChangedEventHandler(MainPage_SizeChanged);

			// Start a periodic timer to keep the controls up to date.
			timer = new System.Windows.Threading.DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(0.5);
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}


		void MediaControls_GoFullscreen (object sender, EventArgs e) {
			Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
		}
		void MainPage_LayoutUpdated (object sender, EventArgs e) {
			ScalePlayer();
		}
		void MainPage_SizeChanged (object sender, SizeChangedEventArgs e) {
			ScalePlayer();
		}

		void MediaPlayer_MediaFailed (object sender, ExceptionRoutedEventArgs e) {
			throw e.ErrorException; // this should hit the JavaScript controler and cause WMP to be loaded.
		}

		void MediaPlayer_MediaEnded (object sender, RoutedEventArgs e) {
			MediaPlayer.Stop();
			/*Playlist.Seek(TimeSpan.Zero);
			PlayerStreamSource = new HCS_MediaStreamingSource(Playlist);
			MediaPlayer.SetSource(PlayerStreamSource);
			MediaPlayer.Play();

			MediaPlayer.Pause();*/
		}

		private void ScalePlayer () {
			var h = Application.Current.Host.Content.ActualHeight;
			var w = Application.Current.Host.Content.ActualWidth;

			this.Width = w;
			this.Height = h;

			MediaPlayer.Height = h - 64;
			MediaPlayer.Width = w;
			MediaControls.Width = w;
			MediaControls.Margin = new Thickness(0.0, h - 64, 0.0, 0.0);
		}

		void MediaPlayer_MarkerReached (object sender, TimelineMarkerRoutedEventArgs e) {
			// TODO: Subtitles
			// THESE DON'T GET FIRED BY THE MSS.
			// NEED TO ALTER THE PlaylistMgr TO FIRE THESE!

			if (e.Marker.Text.StartsWith("/")) return; //Hack for UK Parliament script events

			// Clean up the caption text
			string cleanMarker = System.Windows.Browser.HttpUtility.HtmlEncode(
				e.Marker.Text
				).Replace("\n","<br/>").Replace("\r","").Replace("'", "&#8217;");
			System.Windows.Browser.HtmlPage.Window.Eval("showTextCaption('" + cleanMarker + "');");
		}

		public void Seek (TimeSpan position) {
			if (timer != null) timer.Stop();
			NeedToReset = true;
			NextSeekLocation = position;
			if (timer != null) timer.Start();
		}

		void MediaControls_PositionMarkerChanged (object sender, WMP_Player_Controls.TimeSpanEvent e) {
			Seek(e.Time);
		}

		public TimeSpan CurrentPosition {
			get {
				return MediaPlayer.Position;
			}
		}

		void timer_Tick (object sender, EventArgs e) {
			if (TimerWorking) return;
			if (PlayerStreamSource == null) return;
			lock (PlayerStreamSource) {
				TimerWorking = true;
				if (NeedToReset) {
					MediaPlayer.Pause(); // must do this!
					Playlist.Seek(NextSeekLocation);
					if (PlayerStreamSource != null) PlayerStreamSource.Dispose();
					PlayerStreamSource = new HCS_MediaStreamingSource(Playlist); // this means all new buffers
					MediaPlayer.SetSource(PlayerStreamSource);
					MediaPlayer.Position = NextSeekLocation;
					MediaPlayer.Play();
					NeedToReset = false;
				} else { // not a reset pass.
					MediaControls.UpdatePosition(
						Playlist,
						CurrentPosition,
						MediaPlayer.CurrentState);
				}
				TimerWorking = false;
			}
		}

		void MediaControls_VolumeChanged (object sender, EventArgs e) {
			MediaPlayer.Volume = (double)MediaControls.Volume / 100.0;
		}

		void MediaControls_JumpForward (object sender, EventArgs e) {
			//WindOn(15 /*seconds*/);
			Seek(Playlist.PlaylistDuration);
		}

		void MediaControls_JumpBack (object sender, EventArgs e) {
			//WindOn(-15 /*seconds*/);
			Seek(TimeSpan.Zero);
		}

		void MediaControls_Rewind (object sender, EventArgs e) {
			WindOn(-60 /*seconds*/);
		}

		void MediaControls_FastForward (object sender, EventArgs e) {
			WindOn(60 /*seconds*/);
		}

		private void WindOn (int seconds) {
			if (MediaPlayer.CurrentState != MediaElementState.Playing) return;

			TimeSpan t = CurrentPosition.Add(TimeSpan.FromSeconds(seconds));
			if (t < TimeSpan.Zero) t = TimeSpan.Zero;
			if (t > Playlist.PlaylistDuration) t = Playlist.PlaylistDuration;
			Seek(t);
		}

		void MediaControls_StopVideo (object sender, EventArgs e) {
			MediaPlayer.Stop();
		}

		void MediaControls_TogglePlay (object sender, EventArgs e) {
			switch (MediaPlayer.CurrentState) {
				case MediaElementState.AcquiringLicense:
				case MediaElementState.Buffering:
				case MediaElementState.Individualizing:
				case MediaElementState.Opening:
				case MediaElementState.Playing:
					Pause();
					break;

				case MediaElementState.Paused:
				case MediaElementState.Stopped:
					Play();
					break;
			}
		}

		/// <summary>
		/// Update controls for a change in the player state
		/// </summary>
		void MediaPlayer_CurrentStateChanged (object sender, RoutedEventArgs e) {
			switch (MediaPlayer.CurrentState) {
				case MediaElementState.AcquiringLicense:
				case MediaElementState.Buffering:
				case MediaElementState.Individualizing:
				case MediaElementState.Opening:
				case MediaElementState.Playing:
					MediaControls.IsPlaying = true;
					break;

				case MediaElementState.Paused:
				case MediaElementState.Stopped:
					MediaControls.IsPlaying = false;
					break;
			}
		}


	}
}