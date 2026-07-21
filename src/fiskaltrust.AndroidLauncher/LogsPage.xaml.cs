using System.IO;
using Android.Content;
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher;

public class LogDayItem
{
	public string Label { get; set; } = "";
	public bool IsCurrent { get; set; }
}

public partial class LogsPage : ContentPage
{
	IDispatcherTimer _timer;
	List<FileInfo> _logFiles = new();
	List<LogDayItem> _dayItems = new();
	int _currentIndex = -1;
	bool _drawerExpanded;
	bool _isFollowing = true;

	public LogsPage()
	{
		InitializeComponent();
		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.IsRepeating = true;

		_timer.Tick += async (_, __) => await OnTick(false);
	}

	private FileInfo? SelectedLogFile =>
		_currentIndex >= 0 && _currentIndex < _logFiles.Count ? _logFiles[_currentIndex] : null;

	private void RefreshLogFileList()
	{
		_logFiles = FileLoggerHelper.GetLogFilesOrderedByDateDescending();
		_currentIndex = _logFiles.Count > 0 ? 0 : -1;
		RebuildDayItems();
	}

	private void RebuildDayItems()
	{
		_dayItems = _logFiles.Select((f, i) => new LogDayItem
		{
			Label = f.LastWriteTime.ToString("yyyy-MM-dd"),
			IsCurrent = i == _currentIndex
		}).ToList();

		DateList.ItemsSource = _dayItems;
		DateLabel.Text = _currentIndex >= 0 ? _dayItems[_currentIndex].Label : "";
	}

	private async Task OnTick(bool forceFollow = false)
	{
		var selectedFile = SelectedLogFile;
		if (selectedFile == null) return;

		bool init = string.IsNullOrEmpty(LogView.Text);
		bool follow = forceFollow || _isFollowing;

		var text = FileLoggerHelper.GetLastLines(selectedFile, 1024);

		await Dispatcher.DispatchAsync(() => LogView.Text = text);
		if (init || follow)
		{
			await Dispatcher.DispatchAsync(() => Scroll.ScrollToAsync(Scroll.ScrollX, Math.Max(0, Scroll.Content.Height - Scroll.Height), false));
		}
	}

	private void OnScrollScrolled(object sender, ScrolledEventArgs e)
	{
		const double bottomTolerance = 24;
		_isFollowing = e.ScrollY >= Scroll.Content.Height - Scroll.Height - bottomTolerance;
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

	private void OnBarTapped(object sender, EventArgs e) => SetDrawer(!_drawerExpanded);

	private void OnBarSwiped(object sender, SwipedEventArgs e)
	{
		if (e.Direction == SwipeDirection.Down) SetDrawer(true);
		else if (e.Direction == SwipeDirection.Up) SetDrawer(false);
	}

	private void SetDrawer(bool expand)
	{
		if (expand == _drawerExpanded) return;
		_drawerExpanded = expand;

		var currentHeight = DrawerBodyRow.Height.Value;
		var targetHeight = expand ? 320 : 0;

		if (expand)
		{
			DrawerBody.IsVisible = true;
		}

		new Animation(v => DrawerBodyRow.Height = new GridLength(v), currentHeight, targetHeight)
			.Commit(this, "DrawerAnimation", 16, 250, Easing.CubicInOut, (v, cancelled) =>
			{
				if (!expand)
				{
					DrawerBody.IsVisible = false;
				}
			});

		ChevronIcon.RotateTo(expand ? 180 : 0, 200, Easing.CubicInOut);
	}

	private async void OnDateRowTapped(object sender, EventArgs e)
	{
		if (sender is not Element element || element.BindingContext is not LogDayItem tapped) return;

		var index = _dayItems.IndexOf(tapped);
		if (index < 0 || index == _currentIndex) return;

		_currentIndex = index;
		_isFollowing = true;
		RebuildDayItems();
		await OnTick(true);
	}

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

		SetDrawer(false);
		await DisplayAlert("Success", "The log files were saved to the selected folder.", "OK");
	}

	private static string GetUniqueLogFileName(string originalName)
	{
		return $"{Path.GetFileNameWithoutExtension(originalName)}_{DateTime.Now:yyyyMMdd_HHmmssfff}{Path.GetExtension(originalName)}";
	}
}
