using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HCS.StreamSource;

namespace TestPlayer {
	public partial class WMP_Player_Controls : UserControl {
		public class TimeSpanEvent : EventArgs {
			public TimeSpan Time { get; set; }
		}


		public WMP_Player_Controls () {
			// Required to initialize variables
			InitializeComponent();
			IsPlaying = false;
			ChunkSlider.NewChunkSelected += ChunkSlider_NewChunkSelected;
		}

		void ChunkSlider_NewChunkSelected (object sender, ClickAnywhereSlider.ChunkChangeEventArgs e) {
			if (_isUpdating) return; // don't get into a feedback loop!

			TimeTextBox.Text = "Seeking ";
			if (_lastTotalPlayLength > TimeSpan.Zero) { // try to guess seek location, as a hint to user
				TimeTextBox.Text += FormatTimeSpan(ChunkSlider.Playhead);
			}

			var ne = new TimeSpanEvent { Time = ChunkSlider.Playhead };
			OnPositionMarkerChanged(this, ne);
		}

		private bool _isUpdating; // nasty hack to get around inter-dependant event loops.
		private TimeSpan _lastTotalPlayLength = TimeSpan.Zero;
		public void UpdatePosition (PlaylistMgr Playlist, TimeSpan CurrentPosition, MediaElementState State) {
			if (_isUpdating) return;
			_isUpdating = true;
			int AvailableChunkCount = Playlist.AvailableChunkCount;
			_lastTotalPlayLength = Playlist.PlaylistDuration;

			ChunkSlider.AvailableChunkCount = AvailableChunkCount;
			ChunkSlider.ChunkDuration = Playlist.ChunkDuration;

			if (CurrentPosition == TimeSpan.Zero) {
				TimeTextBox.Text = "Loading";
			} else {
				TimeTextBox.Text = State + "  " + FormatTimeSpan(CurrentPosition);
				ChunkSlider.UpdatePlayhead(Playlist, CurrentPosition);
			}
			_isUpdating = false;
		}

		private static string FormatTimeSpan (TimeSpan position) {
			string val = "";
			if (position.TotalHours >= 1.0) {
				val += ((position.Days * 24) + position.Hours) + ":"; // if it goes into years, you have bigger problems than the string format...
			}
			val += position.Minutes.ToString("00") + ":" + position.Seconds.ToString("00");
			return val;
		}

		public event EventHandler TogglePlay;
		public event EventHandler StopVideo;
		public event EventHandler JumpBack;
		public event EventHandler JumpForward;
		public event EventHandler FastForward;
		public event EventHandler Rewind;
		public event EventHandler VolumeChanged;
		public event EventHandler GoFullscreen;
		public event EventHandler<TimeSpanEvent> PositionMarkerChanged;

		protected void OnTogglePlay (object sender, EventArgs e) {
			if (TogglePlay != null) {
				TogglePlay(sender, e);
			}
		}
		protected void OnFastForward (object sender, EventArgs e) {
			if (FastForward != null) {
				FastForward(sender, e);
			}
		}
		protected void OnRewind (object sender, EventArgs e) {
			if (Rewind != null) {
				Rewind(sender, e);
			}
		}
		protected void OnStopVideo (object sender, EventArgs e) {
			if (StopVideo != null) {
				StopVideo(sender, e);
			}
		}
		protected void OnJumpBack (object sender, EventArgs e) {
			if (JumpBack != null) {
				JumpBack(sender, e);
			}
		}
		protected void OnJumpForward (object sender, EventArgs e) {
			if (JumpForward != null) {
				JumpForward(sender, e);
			}
		}
		protected void OnVolumeChanged (object sender, EventArgs e) {
			if (VolumeChanged != null) {
				VolumeChanged(sender, e);
			}
		}
		protected void OnGoFullscreen (object sender, EventArgs e) {
			if (GoFullscreen != null) {
				GoFullscreen(sender, e);
			}
		}
		protected void OnPositionMarkerChanged (object sender, TimeSpanEvent e) {
			if (PositionMarkerChanged != null) {
				PositionMarkerChanged(sender, e);
				ChunkSlider.Continue();
			}
		}

		public bool IsPlaying {
			get { return PlayPause6way.IsActive; }
			set { PlayPause6way.IsActive = value; }
		}

		private int _otherVolume;
		public int Volume {
			get { if (VolumeSlider == null) return 50; return Math.Max((int)VolumeSlider.Value, 0); }
		}

		private void PlayPause6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnTogglePlay(this, new EventArgs());
		}
		private void Stop6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnStopVideo(this, e);
		}
		private void JumpBack6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnJumpBack(this, e);
		}
		private void JumpForward6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnJumpForward(this, e);
		}
		private void Mute6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			if (VolumeSlider.Value > 0.0 && !Mute6way.IsActive) { // currently not muted, store value and mute
				_otherVolume = (int)VolumeSlider.Value;
				VolumeSlider.Value = 0.0;
			} else { // mute is set. Unmute by restoring last saved value
				VolumeSlider.Value = _otherVolume;
			}
		}
		private void Rewind6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnRewind(this, e);
		}
		private void FastForward6way_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnFastForward(this, e);
		}

		private void VolumeSlider_ValueChanged (object sender, EventArgs e) {
			if (VolumeSlider == null) return;
			Mute6way.IsActive = Volume <= 0;
			OnVolumeChanged(this, e);
		}

		private void Fullscreen_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
			OnGoFullscreen(this, e);
		}

	}
}