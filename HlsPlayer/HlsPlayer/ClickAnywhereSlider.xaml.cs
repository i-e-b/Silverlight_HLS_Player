using System;
using System.Windows;
using System.Windows.Controls;
using HCS.StreamSource;

namespace HlsPlayer {
	public partial class ClickAnywhereSlider : UserControl {

		public TimeSpan ChunkDuration { get; set; }
		public int AvailableChunkCount { get; set; }
		public TimeSpan Playhead { get; set; }

		protected PlaylistMgr _playlist;
		protected bool _active;
		protected bool _suspended;
		protected int _lastChunk;

		public ClickAnywhereSlider () {
			InitializeComponent();
		}

		#region Events
		public class ChunkChangeEventArgs:EventArgs{
			/// <summary>1-based index of new chunk</summary>
			public int NewChunkIndex { get; set; }
		}

		public event EventHandler<ChunkChangeEventArgs> NewChunkSelected;

		protected void OnNewChunkSelected (object sender, ChunkChangeEventArgs e) {
			if (NewChunkSelected != null) {
				_suspended = true;
				NewChunkSelected(sender, e);
			}
		}
		#endregion

		public void UpdatePlayhead (PlaylistMgr playlist, TimeSpan NewPosition) {
			if (_suspended) return;
			_playlist = playlist;
			//int chunk = (int)Math.Floor(NewPosition.TotalSeconds / ChunkDuration.TotalSeconds);
			int chunk = _playlist.GetChunkForTime(NewPosition);
			Playhead = NewPosition;

			chunk = Math.Min(AvailableChunkCount - 1, Math.Max(0, chunk));
			_lastChunk = chunk;
			MoveThumbToChunk(_lastChunk);
		}

		private void ClickAnywhereSlider_MouseMove (object sender, System.Windows.Input.MouseEventArgs e) {
			if (!_active) return;
			JumpToPosition(e.GetPosition(SelectionBar).X);
		}

		private void JumpToPosition (double Location) {
			if (!SanityCheck()) return;
			// work out where the thumb should be and move it
			// send change chunk event if appropriate
			var chunk = (int)Math.Floor((Location * AvailableChunkCount) / ActualWidth);
			chunk = Math.Min(AvailableChunkCount - 1, Math.Max(0, chunk));
			MoveThumbToChunk(chunk);

			if (chunk != _lastChunk) {
				_lastChunk = chunk;
				if (_playlist != null) {
					Playhead = _playlist.TimeOfIndex(_lastChunk);
				} else {
					Playhead = new TimeSpan(ChunkDuration.Ticks * _lastChunk);
				}
				OnNewChunkSelected(this, new ChunkChangeEventArgs { NewChunkIndex = _lastChunk+1 });
			}
		}

		private void MoveThumbToChunk (int chunk) {
			double width = ActualWidth;
			double c_prop = width / AvailableChunkCount;
			double thumb_width = Math.Max(32.0, c_prop);
			double err = ((thumb_width - c_prop) / AvailableChunkCount) * chunk;
			double left_margin = (c_prop * chunk) - err;

			// Move the thumb
			Thumb.Width = thumb_width;
			Thumb.Margin = new Thickness(left_margin, 0.0, 0.0, 0.0);
		}

		private bool SanityCheck () {
			if (ChunkDuration < TimeSpan.FromSeconds(1)) return false;
			if (AvailableChunkCount < 2) return false;
			return true;
		}

		private void ClickAnywhereSlider_MouseWheel (object sender, System.Windows.Input.MouseWheelEventArgs e) {			
			// Move a single chunk back or forward
			e.Handled = true;

			_lastChunk -= Math.Sign(e.Delta);
			_lastChunk = Math.Min(AvailableChunkCount - 1, Math.Max(0, _lastChunk));

			MoveThumbToChunk(_lastChunk);
			if (_playlist != null) {
				Playhead = _playlist.TimeOfIndex(_lastChunk);
			} else {
				Playhead = new TimeSpan(ChunkDuration.Ticks * _lastChunk);
			}
			OnNewChunkSelected(this, new ChunkChangeEventArgs { NewChunkIndex = _lastChunk + 1 });
		}

		private void ClickAnywhereSlider_MouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e) {
			_active = CaptureMouse();
			// Jump to current position
			JumpToPosition(e.GetPosition(SelectionBar).X);
		}

		private void ClickAnywhereSlider_MouseLeftButtonUp (object sender, System.Windows.Input.MouseButtonEventArgs e) {
			if (_active) ReleaseMouseCapture();
			_active = false;
		}

		/// <summary>
		/// Notifies the chunk slider to continue responding to update events.
		/// This MUST be called after responding to a NewChunkSelected event.
		/// </summary>
		internal void Continue () {
			_suspended = false;
		}
	}
}
