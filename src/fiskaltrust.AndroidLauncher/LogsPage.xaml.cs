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
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.IsRepeating = true;

		_timer.Tick += async (_, __) => await OnTick(false);
	}

	private async Task OnTick(bool follow = false)
	{
		bool init = string.IsNullOrEmpty(LogView.Text);
		double oldHeight = Scroll.Content.Height;
		if (Scroll.ScrollY == oldHeight - Scroll.Height)
		{
			follow = true;
		}

		await Dispatcher.DispatchAsync(() => LogView.Text = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024));
		if (init || (follow && oldHeight != Scroll.Content.Height))
		{
			await Dispatcher.DispatchAsync(() => Scroll.ScrollToAsync(Scroll.ScrollX, Scroll.Content.Height - Scroll.Height, !init));
		}
	}

	private void OnAppearing(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(async () =>
		{
			await OnTick(true);
			_timer.Start();
		});
	}

	private void OnDisappearing(object sender, EventArgs e)
	{
		_timer.Stop();
	}
}

