using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AndroidX.DocumentFile.Provider;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher;

public class LogLineItem
{
	public string Text { get; init; } = "";
	public bool IsBanded { get; init; }
}

public class LogLineTemplateSelector : DataTemplateSelector
{
	readonly DataTemplate _plain;
	readonly DataTemplate _banded;

	public LogLineTemplateSelector(BindableObject offsetSource)
	{
		_plain = Create(offsetSource, false);
		_banded = Create(offsetSource, true);
	}

	protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item is LogLineItem { IsBanded: true } ? _banded : _plain;

	private static DataTemplate Create(BindableObject offsetSource, bool banded) => new(() =>
	{
		var label = new Label
		{
			LineHeight = 1.463,
			Padding = new Thickness(16, 4),
			MaxLines = 1,
			LineBreakMode = LineBreakMode.TailTruncation,
		};
		label.SetBinding(Label.TextProperty, static (LogLineItem item) => item.Text);
		label.SetBinding(Label.MarginProperty, new Binding("LineMargin", source: offsetSource));
		label.SetBinding(Label.TranslationXProperty, new Binding("LineTranslation", source: offsetSource));
		if (banded)
		{
			var resources = Application.Current!.Resources;
			label.SetAppThemeColor(Label.BackgroundColorProperty, (Color)resources["FtGround"], (Color)resources["FtGroundNight"]);
		}
		return label;
	});
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
	double _widestLine;
	double _lineOffset;
	bool _widthPending;
	Android.Graphics.Paint? _linePaint;

	public Thickness LineMargin { get; private set; }
	public double LineTranslation => -_lineOffset;

	public LogsPage()
	{
		InitializeComponent();
		LogView.ItemTemplate = new LogLineTemplateSelector(this);
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

		DateValue.Text = _selectedDate == default ? "" : _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		DateFieldArea.IsVisible = hasLogs;
		DateFieldArea.Padding = new Thickness(16, 16, 16, file == null ? 16 : 0);
		CountRow.IsVisible = file != null;
		ListTopBorder.IsVisible = file != null;
		LogView.IsVisible = file != null;
		EmptyState.IsVisible = file == null;
		LogsAppBar.TrailingGlyph = hasLogs ? FaIcons.Gear : null;

		if (file != null) return;

		_logLines.Clear();
		ResetLineWidth();
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

		ResetLineWidth();
		var items = lines.Select(CreateLine).ToList();
		foreach (var item in items.OrderByDescending(i => i.Text.Length).Take(20))
		{
			TrackLineWidth(item.Text);
		}
		if (_newestFirst) items.Reverse();
		SetLines(items);

		_isTruncated = _totalLines > MaxInitialLines;
		TruncatedLogText.Text = $"Showing the last {lines.Count} of {_totalLines} lines in this file.";
		PlaceBanner();

		_readOffset = file.Length;
		_loadedFilePath = file.FullName;
		UpdateCount();
	}

	private const int MaxDisplayedChars = 1000;

	private void SetLines(List<LogLineItem> items)
	{
		_logLines = new ObservableCollection<LogLineItem>(items);
		LogView.ItemsSource = _logLines;
	}

	private LogLineItem CreateLine(string text)
	{
		var display = text.Length > MaxDisplayedChars ? text[..MaxDisplayedChars] + "…" : text;
		return new() { Text = display, IsBanded = _nextLineNumber++ % 2 == 1 };
	}

	private void ResetLineWidth()
	{
		_widestLine = 0;
		_lineOffset = 0;
		OnPropertyChanged(nameof(LineTranslation));
		ApplyListWidth();
	}

	private void TrackLineWidth(string text)
	{
		try
		{
			var metrics = Platform.AppContext.Resources!.DisplayMetrics!;
			if (_linePaint == null)
			{
				_linePaint = new Android.Graphics.Paint { TextSize = 14 * metrics.ScaledDensity };
				_linePaint.SetTypeface(Android.Graphics.Typeface.CreateFromAsset(Platform.AppContext.Assets, "Roboto-Regular.ttf"));
			}
			var width = _linePaint.MeasureText(text) / metrics.Density + 40;
			if (width <= _widestLine) return;
			_widestLine = width;
			if (_widthPending) return;
			_widthPending = true;
			Dispatcher.Dispatch(() =>
			{
				_widthPending = false;
				ApplyListWidth();
			});
		}
		catch (Exception ex)
		{
			Android.Util.Log.Warn("LogsPage", $"Failed to measure log line: {ex.Message}");
		}
	}

	private double MaxLineOffset => Math.Max(0, _widestLine - (LogView.Width > 0 ? LogView.Width : Width));

	private void ApplyListWidth()
	{
		var margin = new Thickness(0, 0, -MaxLineOffset, 0);
		if (margin != LineMargin)
		{
			LineMargin = margin;
			OnPropertyChanged(nameof(LineMargin));
		}
		SetLineOffset(_lineOffset);
	}

	private void SetLineOffset(double offset)
	{
		offset = Math.Clamp(offset, 0, MaxLineOffset);
		if (offset == _lineOffset) return;
		_lineOffset = offset;
		OnPropertyChanged(nameof(LineTranslation));
	}

	private void OnLogViewSizeChanged(object? sender, EventArgs e) => ApplyListWidth();

	private void OnLogViewHandlerChanged(object? sender, EventArgs e)
	{
		if (LogView.Handler?.PlatformView is not RecyclerView recycler || recycler.Context == null) return;
		var density = recycler.Context.Resources!.DisplayMetrics!.Density;
		recycler.AddOnItemTouchListener(new HorizontalDragListener(recycler.Context, dx => SetLineOffset(_lineOffset + dx / density)));
		recycler.AddItemDecoration(new HairlineDecoration((int)Math.Max(1, density), IsLogLinePosition));
	}

	private bool IsLogLinePosition(int position)
	{
		if (LogView.Header != null) position--;
		return position >= 0 && position < _logLines.Count;
	}

	private sealed class HairlineDecoration : RecyclerView.ItemDecoration
	{
		readonly int _height;
		readonly Func<int, bool> _isLine;
		readonly Android.Graphics.Paint _paint = new();

		public HairlineDecoration(int height, Func<int, bool> isLine)
		{
			_height = height;
			_isLine = isLine;
		}

		public override void GetItemOffsets(Android.Graphics.Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
		{
			outRect.Set(0, 0, 0, _isLine(parent.GetChildAdapterPosition(view)) ? _height : 0);
		}

		public override void OnDraw(Android.Graphics.Canvas c, RecyclerView parent, RecyclerView.State state)
		{
			var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
			_paint.Color = new Android.Graphics.Color(((Color)Application.Current!.Resources[dark ? "FtHairlineNight" : "FtHairline"]).ToInt());
			for (var i = 0; i < parent.ChildCount; i++)
			{
				var child = parent.GetChildAt(i);
				if (child == null || !_isLine(parent.GetChildAdapterPosition(child))) continue;
				var top = child.Bottom + (int)child.TranslationY;
				c.DrawRect(0, top, parent.Width, top + _height, _paint);
			}
		}
	}

	private sealed class HorizontalDragListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
	{
		readonly Action<float> _onDrag;
		readonly int _touchSlop;
		float _startX;
		float _startY;
		float _lastX;
		bool _dragging;

		public HorizontalDragListener(Context context, Action<float> onDrag)
		{
			_onDrag = onDrag;
			_touchSlop = ViewConfiguration.Get(context)!.ScaledTouchSlop;
		}

		public bool OnInterceptTouchEvent(RecyclerView recyclerView, MotionEvent e)
		{
			switch (e.ActionMasked)
			{
				case MotionEventActions.Down:
					_startX = _lastX = e.GetX();
					_startY = e.GetY();
					_dragging = false;
					break;
				case MotionEventActions.Move when !_dragging:
					var dx = e.GetX() - _startX;
					var dy = e.GetY() - _startY;
					if (Math.Abs(dx) > _touchSlop && Math.Abs(dx) > Math.Abs(dy))
					{
						_dragging = true;
						_lastX = e.GetX();
						recyclerView.Parent?.RequestDisallowInterceptTouchEvent(true);
					}
					break;
				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					_dragging = false;
					break;
			}
			return _dragging;
		}

		public void OnTouchEvent(RecyclerView recyclerView, MotionEvent e)
		{
			switch (e.ActionMasked)
			{
				case MotionEventActions.Move:
					var x = e.GetX();
					_onDrag(_lastX - x);
					_lastX = x;
					break;
				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					_dragging = false;
					break;
			}
		}

		public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept)
		{
		}
	}

	private void UpdateCount()
	{
		CountLabel.Text = _totalLines == 1 ? "1 line" : $"{_totalLines} lines";
		SemanticProperties.SetDescription(SortButton, _newestFirst ? "Sort order: newest first" : "Sort order: oldest first");
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
						var item = CreateLine(line);
						TrackLineWidth(item.Text);
						if (_newestFirst) _logLines.Insert(0, item);
						else _logLines.Add(item);
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

	private void OnSortTapped(object? sender, EventArgs e)
	{
		CloseMenus();
		_newestFirst = !_newestFirst;
		UpdateCount();

		SetLines(_logLines.Reverse().ToList());

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
		var canExportCurrent = SelectedLogFile != null;
		var itemColor = canExportCurrent ? "FtTextSecondary" : "FtTextDisabled";
		ExportCurrentTouch.IsVisible = canExportCurrent;
		ExportCurrentIcon.SetAppThemeColor(Label.TextColorProperty, (Color)Application.Current!.Resources[itemColor], (Color)Application.Current.Resources[itemColor + "Night"]);
		ExportCurrentLabel.SetAppThemeColor(Label.TextColorProperty, (Color)Application.Current.Resources[itemColor], (Color)Application.Current.Resources[itemColor + "Night"]);
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
		var logDates = _logFiles.Select(f => f.LastWriteTime.Date).ToHashSet();
		Calendar.HasLogs = logDates.Contains;
		Calendar.Open(_selectedDate == default ? DateTime.Today : _selectedDate);
		Calendar.IsVisible = true;
		MenuOverlay.IsVisible = true;
		SetDateFieldFocused(true);
	}

	private void OnCalendarDateSelected(object? sender, DateTime date)
	{
		if (date == _selectedDate) return;

		_selectedDate = date;
		_isFollowing = true;
		RefreshLogFileList();
		OnTick(true);
	}

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
