using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Android.Content;
using AndroidX.DocumentFile.Provider;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher;

public class LogLineItem
{
	public string Text { get; init; } = "";
	public bool IsBanded { get; init; }
}

public partial class LogsPage : ContentPage
{
	IDispatcherTimer _timer;
	List<FileInfo> _logFiles = new();
	ObservableCollection<LogLineItem> _logLines = new();
	DateTime _selectedDate;
	bool _isFollowing = true;
	bool _newestFirst;
	string? _loadedFilePath;
	long _readOffset;
	long _nextLineNumber;
	long _totalLines;
	bool _isTruncated;

	public LogsPage()
	{
		InitializeComponent();
		LogView.ItemsSource = _logLines;
		LogView.Header = null;

		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.IsRepeating = true;

		_timer.Tick += (_, __) => OnTick(false);
	}

	private FileInfo? SelectedLogFile => _logFiles.FirstOrDefault(f => f.LastWriteTime.Date == _selectedDate);

	private void RefreshLogFileList()
	{
		_logFiles = FileLoggerHelper.GetLogFilesOrderedByDateDescending();

		if (_selectedDate == default && _logFiles.Count > 0)
		{
			_selectedDate = _logFiles[0].LastWriteTime.Date;
		}

		UpdateView();
	}

	private void UpdateView()
	{
		var hasLogs = _logFiles.Count > 0;
		var file = SelectedLogFile;

		DateValue.Text = _selectedDate == default ? "" : _selectedDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
		DateFieldArea.IsVisible = hasLogs;
		CountRow.IsVisible = file != null;
		ListTopBorder.IsVisible = file != null;
		LogView.IsVisible = file != null;
		EmptyState.IsVisible = file == null;
		LogsAppBar.TrailingGlyph = hasLogs ? FaIcons.Gear : null;

		if (file != null) return;

		_logLines.Clear();
		_loadedFilePath = null;
		_isTruncated = false;
		PlaceBanner();

		if (!hasLogs)
		{
			StateIcon.Text = FaIcons.FileLines;
			StateTitle.Text = "No logs yet";
			StateBody.Text = "Logs appear here once the Middleware starts handling requests from the POS.";
			StateAction.IsVisible = false;
		}
		else
		{
			StateIcon.Text = FaIcons.CalendarDays;
			StateTitle.Text = "No logs for this date";
			StateBody.Text = $"Nothing was recorded on {DateValue.Text}. Choose another date to keep looking.";
			StateAction.IsVisible = true;
		}
	}

	private const int MaxInitialLines = 1024;

	private void LoadInitialContent(FileInfo file)
	{
		var lines = FileLoggerHelper.SplitIntoLines(FileLoggerHelper.GetLastLines(file, MaxInitialLines)).ToList();
		_totalLines = FileLoggerHelper.CountLines(file);
		_nextLineNumber = Math.Max(0, _totalLines - lines.Count);

		_logLines.Clear();
		var items = lines.Select(CreateLine).ToList();
		if (_newestFirst) items.Reverse();
		foreach (var item in items)
		{
			_logLines.Add(item);
		}

		_isTruncated = _totalLines > MaxInitialLines;
		TruncatedLogText.Text = $"Showing the last {lines.Count} of {_totalLines} lines in this file.";
		PlaceBanner();

		_readOffset = file.Length;
		_loadedFilePath = file.FullName;
		UpdateCount();
	}

	private LogLineItem CreateLine(string text) => new() { Text = text, IsBanded = _nextLineNumber++ % 2 == 1 };

	private void UpdateCount()
	{
		CountLabel.Text = _totalLines == 1 ? "1 line" : $"{_totalLines} lines";
	}

	private void OnTick(bool forceFollow = false)
	{
		try
		{
			var selectedFile = SelectedLogFile;
			if (selectedFile == null) return;

			bool follow = forceFollow || _isFollowing;
			bool contentChanged = false;

			if (selectedFile.FullName != _loadedFilePath)
			{
				LoadInitialContent(selectedFile);
				follow = true;
				contentChanged = true;
			}
			else
			{
				var offset = _readOffset;
				var newLines = FileLoggerHelper.ReadNewLines(selectedFile, ref offset);
				_readOffset = offset;
				if (newLines.Count > 0)
				{
					foreach (var line in newLines)
					{
						if (_newestFirst) _logLines.Insert(0, CreateLine(line));
						else _logLines.Add(CreateLine(line));
					}
					_totalLines += newLines.Count;
					UpdateCount();
					contentChanged = true;
				}
			}

			if (follow && contentChanged && _logLines.Count > 0)
			{
				ScrollToNewest();
			}
		}
		catch (Exception ex)
		{
			Android.Util.Log.Warn("LogsPage", $"Failed to refresh log view: {ex.Message}");
		}
	}

	private void ScrollToNewest()
	{
		ScrollToNewestNow();
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), ScrollToNewestNow);
	}

	private void ScrollToNewestNow()
	{
		if (_logLines.Count == 0) return;
		if (_newestFirst) LogView.ScrollTo(_logLines[0], position: ScrollToPosition.Start, animate: false);
		else LogView.ScrollTo(_logLines[^1], position: ScrollToPosition.End, animate: false);
	}

	private void PlaceBanner()
	{
		LogView.Header = null;
		LogView.Footer = null;
		if (!_isTruncated) return;
		if (_newestFirst) LogView.Footer = TruncatedLogBanner;
		else LogView.Header = TruncatedLogBanner;
	}

	private void OnLogViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.LastVisibleItemIndex < 0) return;

		const int toleranceItems = 2;
		_isFollowing = _newestFirst
			? e.FirstVisibleItemIndex <= toleranceItems
			: e.LastVisibleItemIndex >= _logLines.Count - 1 - toleranceItems;
	}

	private void OnSortTapped(object sender, TappedEventArgs e)
	{
		_newestFirst = !_newestFirst;
		SemanticProperties.SetDescription(SortButton, _newestFirst ? "Oldest first" : "Newest first");

		var reversed = _logLines.Reverse().ToList();
		_logLines.Clear();
		foreach (var item in reversed)
		{
			_logLines.Add(item);
		}

		PlaceBanner();
		_isFollowing = true;
		if (_logLines.Count > 0) ScrollToNewest();
	}

	private void OnAppearing(object sender, EventArgs e)
	{
		App.Resumed += OnAppResumed;
		RefreshLogFileList();
		Dispatcher.Dispatch(() =>
		{
			OnTick(true);
			_timer.Start();
		});
	}

	private void OnDisappearing(object sender, EventArgs e)
	{
		App.Resumed -= OnAppResumed;
		_timer.Stop();
		CloseMenus();
	}

	private void OnAppResumed()
	{
		Dispatcher.Dispatch(() =>
		{
			RefreshLogFileList();
			OnTick(true);
		});
	}

	private void OnSettingsTapped(object? sender, EventArgs e)
	{
		if (ActionsMenu.IsVisible)
		{
			CloseMenus();
			return;
		}

		CloseMenus();
		ExportCurrentItem.IsEnabled = SelectedLogFile != null;
		ExportCurrentItem.Opacity = SelectedLogFile != null ? 1 : 0.38;
		ActionsMenu.IsVisible = true;
		MenuOverlay.IsVisible = true;
	}

	private void OnDateFieldTapped(object sender, TappedEventArgs e)
	{
		if (Calendar.IsVisible)
		{
			CloseMenus();
			return;
		}

		OpenCalendar();
	}

	private void OnChangeDateClicked(object sender, EventArgs e) => OpenCalendar();

	private void OpenCalendar()
	{
		CloseMenus();
		Calendar.Open(_selectedDate == default ? DateTime.Today : _selectedDate);
		Calendar.IsVisible = true;
		MenuOverlay.IsVisible = true;
		SetDateFieldFocused(true);
	}

	private void OnCalendarConfirmed(object? sender, DateTime date)
	{
		CloseMenus();
		if (date == _selectedDate) return;

		_selectedDate = date;
		_isFollowing = true;
		RefreshLogFileList();
		OnTick(true);
	}

	private void OnCalendarCancelled(object? sender, EventArgs e) => CloseMenus();

	private void OnMenuOverlayTapped(object sender, TappedEventArgs e) => CloseMenus();

	private void CloseMenus()
	{
		ActionsMenu.IsVisible = false;
		Calendar.IsVisible = false;
		MenuOverlay.IsVisible = false;
		SetDateFieldFocused(false);
	}

	private void SetDateFieldFocused(bool focused)
	{
		var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
		string Key(string name) => dark ? name + "Night" : name;

		DateField.StrokeThickness = focused ? 2 : 1;
		DateField.Padding = focused ? new Thickness(15, 0, 3, 0) : new Thickness(16, 0, 4, 0);
		DateField.Stroke = (Color)Application.Current!.Resources[Key(focused ? "FtPrimary" : "FtBorder")];
		DateFieldLabel.TextColor = (Color)Application.Current.Resources[Key(focused ? "FtPrimary" : "FtTextSecondary")];
	}

	private async void OnExportCurrentClicked(object sender, EventArgs e)
	{
		CloseMenus();

		var selectedFile = SelectedLogFile;
		if (selectedFile == null) return;

		await ExportFilesAsync(new[] { selectedFile });
	}

	private async void OnExportAllClicked(object sender, EventArgs e)
	{
		CloseMenus();
		await ExportFilesAsync(FileLoggerHelper.GetLogFiles());
	}

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

		Android.Widget.Toast.MakeText(activity, "Logs saved to the selected folder.", Android.Widget.ToastLength.Short)?.Show();
	}

	private static string GetUniqueLogFileName(string originalName)
	{
		return $"{Path.GetFileNameWithoutExtension(originalName)}_{DateTime.Now:yyyyMMdd_HHmmssfff}{Path.GetExtension(originalName)}";
	}
}
