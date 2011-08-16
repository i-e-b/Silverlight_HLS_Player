using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HlsPlayer.Images {
	public partial class SixWayButton : UserControl {

		private BitmapImage[] imgs = new BitmapImage[6];

		public string Normal_Inactive {
			get { return imgs[0].UriSource.ToString(); }
			set {
				imgs[0] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}
		public string Hover_Inactive {
			get { return imgs[1].UriSource.ToString(); }
			set {
				imgs[1] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}
		public string Press_Inactive {
			get { return imgs[2].UriSource.ToString(); }
			set {
				imgs[2] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}

		public string Normal_Active {
			get { return imgs[3].UriSource.ToString(); }
			set {
				imgs[3] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}
		public string Hover_Active {
			get { return imgs[4].UriSource.ToString(); }
			set {
				imgs[4] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}
		public string Press_Active {
			get { return imgs[5].UriSource.ToString(); }
			set {
				imgs[5] = new BitmapImage(new Uri(value, UriKind.Relative));
				SwitchState();
			}
		}

		private bool _isActive;
		public bool IsActive {
			get { return _isActive; }
			set {
				_isActive = value;
				SwitchState();
			}
		}

		private int _state;

		public SixWayButton () {
			// Required to initialize variables
			InitializeComponent();
			_state = 0;
			_isActive = false;
			SwitchState();
		}

		private void SwitchState () {
			try {
				BitmapImage src = null;
				switch (_state) {
					case 0:
						src = (IsActive) ? (imgs[3]) : (imgs[0]);
						if (src != null) Current.Source = src;
						break;

					case 1:
						src = (IsActive) ? (imgs[4]) : (imgs[1]);
						if (src != null) Current.Source = src;
						break;

					case 2:
						src = (IsActive) ? (imgs[5]) : (imgs[2]);
						if (src != null) Current.Source = src;
						break;
				}
			} catch { };
		}

		private void UserControl_MouseEnter (object sender, System.Windows.Input.MouseEventArgs e) {
			_state = 1;
			SwitchState();
			//Current.Source = new BitmapImage(new Uri((IsActive) ? (Hover_Active) : (Hover_Inactive),UriKind.Relative));
		}

		private void UserControl_MouseLeave (object sender, System.Windows.Input.MouseEventArgs e) {
			_state = 0;
			SwitchState();
			//Current.Source = new BitmapImage(new Uri((IsActive) ? (Normal_Active) : (Normal_Inactive),UriKind.Relative));
		}

		private void UserControl_MouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e) {
			_state = 2;
			SwitchState();
			//Current.Source = new BitmapImage(new Uri((IsActive) ? (Press_Active) : (Press_Inactive),UriKind.Relative));
		}

		private void UserControl_MouseLeftButtonUp (object sender, System.Windows.Input.MouseButtonEventArgs e) {
			_state = 1;
			SwitchState();
			//Current.Source = new BitmapImage(new Uri((IsActive) ? (Normal_Active) : (Normal_Inactive),UriKind.Relative));
		}
	}
}