using System.IO;
using Android.Content;
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using fiskaltrust.AndroidLauncher.Helpers;
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

		var text = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024);

		await Dispatcher.DispatchAsync(() =>
		{
			LogView.Text = text;
			ClearLogsMenuItem.IsEnabled = !string.IsNullOrEmpty(text);
		});
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

	private async void OnSaveLogsClicked(object sender, EventArgs e) => await SaveLogsAsync();

	private async void OnClearLogsClicked(object sender, EventArgs e) => await ClearLogsAsync();

	private async Task ClearLogsAsync()
	{
		var confirmed = await DisplayAlert("Clear Logs", "This permanently deletes the log history on this device. It can't be undone.\nMake sure you've saved any logs you want to keep before continuing.", "Clear", "Cancel");
		if (!confirmed) return;

		FileLoggerHelper.ClearCurrentLogFile();
		LogView.Text = string.Empty;
	}

	private async Task SaveLogsAsync()
	{
		var activity = Platform.CurrentActivity;
		if (activity == null) return;

		var tcs = new TaskCompletionSource<Intent?>();
		ActivityResultBridge.PendingPickFolderResult = tcs;

		activity.StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), ActivityResultBridge.PickFolderRequestCode);

		var resultIntent = await tcs.Task;
		ActivityResultBridge.PendingPickFolderResult = null;

		var treeUri = resultIntent?.Data;
		if (treeUri == null) return;

		activity.ContentResolver?.TakePersistableUriPermission(treeUri, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

		var pickedDir = DocumentFile.FromTreeUri(activity, treeUri);
		if (pickedDir == null) return;

		foreach (var file in FileLoggerHelper.GetLogFiles())
		{
			var newDoc = pickedDir.CreateFile("text/plain", GetUniqueLogFileName(file.Name));
			if (newDoc?.Uri == null) continue;

			using var output = activity.ContentResolver?.OpenOutputStream(newDoc.Uri);
			if (output == null) continue;

			using var input = file.OpenRead();
			await input.CopyToAsync(output);
		}

		await DisplayAlert("Success", "The log files were saved to the selected folder.", "OK");
	}

	private static string GetUniqueLogFileName(string originalName)
	{
		return $"{Path.GetFileNameWithoutExtension(originalName)}_{DateTime.Now:yyyyMMdd_HHmmssfff}{Path.GetExtension(originalName)}";
	}
}

