using System;
using System.Windows;

namespace TestPlayer {
	public partial class App : Application {

		public App () {
			Startup += Application_Startup;
			Exit += Application_Exit;
			UnhandledException += Application_UnhandledException;

			InitializeComponent();
		}

		private void Application_Startup (object sender, StartupEventArgs e) {
			var p = new MainPage();
			RootVisual = p;

			// Get around some weird scaling issues:
			p.Height = Host.Content.ActualHeight;
			p.Width = Host.Content.ActualWidth;

			// Display the custom initialization parameters.
			foreach (String key in e.InitParams.Keys) {

				switch (key) {
					case "mediaURL":
						p.MediaURL = e.InitParams[key];
						break;

					case "startTime":
						p.PositionString = e.InitParams[key];
						break;

					default: continue;
				}
			}

			p.Play();
		}

		private void Application_Exit (object sender, EventArgs e) {

		}

		private void Application_UnhandledException (object sender, ApplicationUnhandledExceptionEventArgs e) {
			if (!System.Diagnostics.Debugger.IsAttached) {
				e.Handled = true;
				Deployment.Current.Dispatcher.BeginInvoke(() => ReportErrorToDOM(e));
			}
		}

		private void ReportErrorToDOM (ApplicationUnhandledExceptionEventArgs e) {
			try {
				string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

				System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
			} catch (Exception) {
			}
		}
	}
}
