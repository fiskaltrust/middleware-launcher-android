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
		LogView.Text = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024);
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

