using System.Collections.ObjectModel;
using System.IO;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using AndroidX.RecyclerView.Widget;
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
	ObservableCollection<string> _logLines = new();
	int _currentIndex = -1;
	bool _drawerExpanded;
	bool _isFollowing = true;
	int _lastVisibleItemIndex = -1;
	string? _loadedFilePath;
	long _readOffset;
	GestureDetector? _logViewTapDetector;

	public LogsPage()
	{
		InitializeComponent();
		LogView.ItemsSource = _logLines;
		LogView.Header = null;
		LogView.HandlerChanged += (_, __) => AttachLogViewTapToClose();

		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.IsRepeating = true;

		_timer.Tick += async (_, __) => await OnTick(false);
	}

	private void AttachLogViewTapToClose()
	{
		if (FindRecyclerView(LogView.Handler?.PlatformView as Android.Views.View) is not RecyclerView recyclerView) return;

		_logViewTapDetector ??= new GestureDetector(Platform.CurrentActivity, new LogViewTapListener(() =>
		{
			if (_drawerExpanded) SetDrawer(false);
		}));

		recyclerView.AddOnItemTouchListener(new LogViewItemTouchListener(_logViewTapDetector));
	}

	private static RecyclerView? FindRecyclerView(Android.Views.View? view)
	{
		if (view is RecyclerView recyclerView) return recyclerView;
		if (view is ViewGroup group)
		{
			for (var i = 0; i < group.ChildCount; i++)
			{
				var found = FindRecyclerView(group.GetChildAt(i));
				if (found != null) return found;
			}
		}
		return null;
	}

	private sealed class LogViewTapListener : GestureDetector.SimpleOnGestureListener
	{
		readonly Action _onTap;
		public LogViewTapListener(Action onTap) => _onTap = onTap;
		public override bool OnSingleTapUp(MotionEvent e)
		{
			_onTap();
			return true;
		}
	}

	private sealed class LogViewItemTouchListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
	{
		readonly GestureDetector _detector;
		public LogViewItemTouchListener(GestureDetector detector) => _detector = detector;

		public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
		{
			_detector.OnTouchEvent(e);
			return false;
		}

		public void OnTouchEvent(RecyclerView rv, MotionEvent e) { }

		public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept) { }
	}

	private FileInfo? SelectedLogFile =>
		_currentIndex >= 0 && _currentIndex < _logFiles.Count ? _logFiles[_currentIndex] : null;

	private void RefreshLogFileList()
	{
		var previouslySelectedPath = SelectedLogFile?.FullName;

		_logFiles = FileLoggerHelper.GetLogFilesOrderedByDateDescending();

		var preservedIndex = previouslySelectedPath != null
			? _logFiles.FindIndex(f => f.FullName == previouslySelectedPath)
			: -1;

		_currentIndex = preservedIndex >= 0 ? preservedIndex : (_logFiles.Count > 0 ? 0 : -1);
		RebuildDayItems();
	}

	private void RebuildDayItems()
	{
		_dayItems = _logFiles.Select((f, i) => new LogDayItem
		{
			Label = f.LastWriteTime.ToString("yyyy-MM-dd"),
			IsCurrent = i == _currentIndex
		}).ToList();

		BindableLayout.SetItemsSource(DateList, _dayItems);
		DateLabel.Text = _currentIndex >= 0 ? _dayItems[_currentIndex].Label : "";

		var hasLogs = _logFiles.Count > 0;
		DateListScroll.IsVisible = hasLogs;
		ExportButtonRow.IsVisible = hasLogs;
		NoLogsPlaceholder.IsVisible = !hasLogs;

		ExportCurrentButton.IsEnabled = SelectedLogFile != null;
		ExportAllButton.IsEnabled = hasLogs;
	}


	private const int MaxInitialLines = 1024;

	private void LoadInitialContent(FileInfo file)
	{
		_logLines.Clear();
		foreach (var line in FileLoggerHelper.SplitIntoLines(FileLoggerHelper.GetLastLines(file, MaxInitialLines)))
		{
			_logLines.Add(line);
		}

		var totalLines = FileLoggerHelper.CountLines(file);
		var truncated = totalLines > MaxInitialLines;
		if (truncated)
		{
			TruncatedLogText.Text = $"Showing the last {MaxInitialLines} of {totalLines} lines in this file.";
			LogView.Header = TruncatedLogBanner;
		}
		else
		{
			LogView.Header = null;
		}

		_readOffset = file.Length;
		_loadedFilePath = file.FullName;
	}

	private Task OnTick(bool forceFollow = false)
	{
		var selectedFile = SelectedLogFile;
		if (selectedFile == null) return Task.CompletedTask;

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
					_logLines.Add(line);
				}
				contentChanged = true;
			}
		}

		if (follow && contentChanged && _logLines.Count > 0)
		{
			LogView.ScrollTo(_logLines.Count - 1, position: ScrollToPosition.End, animate: false);
		}

		return Task.CompletedTask;
	}

	private void OnLogViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.LastVisibleItemIndex < 0) return;

		_lastVisibleItemIndex = e.LastVisibleItemIndex;

		const int bottomToleranceItems = 2;
		_isFollowing = e.LastVisibleItemIndex >= _logLines.Count - 1 - bottomToleranceItems;
	}

	private void OnAppearing(object sender, EventArgs e)
	{
		App.Resumed += OnAppResumed;
		RefreshLogFileList();
		Dispatcher.Dispatch(async () =>
		{
			await OnTick(true);
			_timer.Start();
		});
	}

	private void OnDisappearing(object sender, EventArgs e)
	{
		App.Resumed -= OnAppResumed;
		_timer.Stop();
	}

	private void OnAppResumed()
	{
		Dispatcher.Dispatch(async () =>
		{
			RefreshLogFileList();
			await OnTick(true);
		});
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

		var wasFollowing = _isFollowing;
		var anchorIndexBeforeResize = _lastVisibleItemIndex;

		if (expand)
		{
			RefreshLogFileList();
			DrawerBody.IsVisible = true;
		}

		var targetHeight = expand ? GetDrawerContentHeight() : 0;

		new Animation(v => DrawerBodyRow.Height = new GridLength(v), currentHeight, targetHeight)
			.Commit(this, "DrawerAnimation", 16, 250, Easing.CubicInOut, (v, cancelled) =>
			{
				if (!expand)
				{
					DrawerBody.IsVisible = false;
				}

				Dispatcher.Dispatch(() => KeepScrollAnchoredToBottom(wasFollowing, anchorIndexBeforeResize));
			});

		ChevronIcon.RotateTo(expand ? 180 : 0, 200, Easing.CubicInOut);
	}

	private double GetDrawerContentHeight()
	{
		const double MaxDrawerHeight = 320;

		if (_logFiles.Count == 0)
		{
			return NoLogsPlaceholder.Measure(Width, double.PositiveInfinity).Height;
		}

		var dayListHeight = DateList.Measure(Width, double.PositiveInfinity).Height;
		var buttonRowHeight = ExportButtonRow.Measure(Width, double.PositiveInfinity).Height;

		return Math.Min(dayListHeight + buttonRowHeight, MaxDrawerHeight);
	}

	private void KeepScrollAnchoredToBottom(bool wasFollowing, int anchorIndexBeforeResize)
	{
		if (_logLines.Count == 0) return;

		if (wasFollowing)
		{
			_isFollowing = true;
			LogView.ScrollTo(_logLines.Count - 1, position: ScrollToPosition.End, animate: false);
			return;
		}

		var anchorIndex = Math.Min(anchorIndexBeforeResize, _logLines.Count - 1);
		if (anchorIndex >= 0)
		{
			LogView.ScrollTo(anchorIndex, position: ScrollToPosition.End, animate: false);
		}
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
