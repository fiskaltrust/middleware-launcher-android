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
	List<FileInfo> _logFiles = new();

	public LogsPage()
	{
		InitializeComponent();
		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.IsRepeating = true;

		_timer.Tick += async (_, __) => await OnTick(false);
	}

	private FileInfo? SelectedLogFile =>
		DateLogPicker.SelectedIndex >= 0 && DateLogPicker.SelectedIndex < _logFiles.Count
			? _logFiles[DateLogPicker.SelectedIndex]
			: null;

	private void RefreshLogFileList()
	{
		_logFiles = FileLoggerHelper.GetLogFilesOrderedByDateDescending();
		DateLogPicker.ItemsSource = _logFiles.Select(f => f.LastWriteTime.ToString("yyyy-MM-dd")).ToList();
		if (_logFiles.Count > 0)
		{
			DateLogPicker.SelectedIndex = 0;
		}
	}

	private async Task OnTick(bool follow = false)
	{
		var selectedFile = SelectedLogFile;
		if (selectedFile == null) return;

		bool init = string.IsNullOrEmpty(LogView.Text);
		double oldHeight = Scroll.Content.Height;
		if (Scroll.ScrollY == oldHeight - Scroll.Height)
		{
			follow = true;
		}

		var text = FileLoggerHelper.GetLastLines(selectedFile, 1024);

		await Dispatcher.DispatchAsync(() => LogView.Text = text);
		if (init || (follow && oldHeight != Scroll.Content.Height))
		{			
			await Dispatcher.DispatchAsync(() => Scroll.ScrollToAsync(Scroll.ScrollX, double.MaxValue, !init));
		}
	}

	private void OnAppearing(object sender, EventArgs e)
	{
		RefreshLogFileList();
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

	private async void OnDateLogPickerSelectedIndexChanged(object sender, EventArgs e) => await OnTick(true);

	private async void OnExportCurrentClicked(object sender, EventArgs e)
	{
		var selectedFile = SelectedLogFile;
		if (selectedFile == null) return;

		await ExportFilesAsync(new[] { selectedFile });
	}

	private async void OnExportAllClicked(object sender, EventArgs e) => await ExportFilesAsync(FileLoggerHelper.GetLogFiles());

	private async Task ExportFilesAsync(IEnumerable<FileInfo> files)
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

		foreach (var file in files)
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
