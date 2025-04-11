using Android.Widget;
using fiskaltrust.AndroidLauncher.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher;

public partial class LogsPage : ContentPage
{
	IDispatcherTimer _timer;

	public LogsPage()
	{
		InitializeComponent();
		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(3);
		_timer.IsRepeating = true;

		_timer.Tick += OnTick;
	}

	private void OnTick(object? sender, EventArgs e)
	{
		bool follow = false;
		bool init = string.IsNullOrEmpty(LogView.Text);
		if (string.IsNullOrEmpty(LogView.Text) || Scroll.ScrollY == Scroll.Content.Height - Scroll.Height)
		{
			follow = true;
		}

		LogView.Text = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024);
		if (follow)
		{
			Dispatcher.Dispatch(() => Scroll.ScrollToAsync(Scroll.ScrollX, Scroll.Content.Height, !init));
		}
	}

	private void OnAppearing(object sender, EventArgs e)
	{
		OnTick(sender, e);
		_timer.Start();
	}

	private void OnDisappearing(object sender, EventArgs e)
	{
		_timer.Stop();
	}
}

