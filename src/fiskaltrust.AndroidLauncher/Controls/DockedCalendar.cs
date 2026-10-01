using System.Globalization;
using fiskaltrust.AndroidLauncher.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace fiskaltrust.AndroidLauncher.Controls;

public class DockedCalendar : ContentView
{
	const double CellSize = 40;
	const double CellGap = 4;
	const double BodyHeight = 7 * CellSize + 5 * CellGap;
	const double ListRowHeight = 56;

	enum PickerMode { Days, Months, Years }

	readonly Button _monthButton;
	readonly Button _yearButton;
	readonly Grid _days;
	readonly VerticalStackLayout _dayView;
	readonly VerticalStackLayout _monthList = new();
	readonly VerticalStackLayout _yearList = new();
	readonly List<PeriodRow> _monthRows = new();
	readonly List<PeriodRow> _yearRows = new();
	(int From, int To) _yearRange;
	readonly ScrollView _listView;
	readonly ContentView _body;
	readonly Button[] _dayButtons = new Button[42];
	readonly DayDots _dots = new();
	readonly GraphicsView _dotsView;
	int _cellsBuilt;
	readonly DateTime[] _dayDates = new DateTime[42];
	DateTime _displayMonth;
	DateTime _pending;
	PickerMode _mode;

	public event EventHandler<DateTime>? DateSelected;

	public Func<DateTime, bool>? HasLogs { get; set; }

	public DockedCalendar()
	{
		_monthButton = PeriodButton(() => ToggleMode(PickerMode.Months), "Choose month");
		_yearButton = PeriodButton(() => ToggleMode(PickerMode.Years), "Choose year");

		var header = new Grid
		{
			HeightRequest = 48,
			ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
		};
		header.Add(Stepper(_monthButton, () => ShiftMonths(-1), () => ShiftMonths(1), "month"), 0, 0);
		header.Add(Stepper(_yearButton, () => ShiftMonths(-12), () => ShiftMonths(12), "year"), 1, 0);

		var weekdays = CreateGrid(1);
		var letters = new[] { "S", "M", "T", "W", "T", "F", "S" };
		for (var i = 0; i < letters.Length; i++)
		{
			var letter = new Label
			{
				Text = letters[i],
				FontFamily = "RobotoMedium",
				FontSize = 14,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};
			letter.SetAppThemeColor(Label.TextColorProperty, Token("FtTextSecondary"), Token("FtTextSecondaryNight"));
			weekdays.Add(letter, i, 0);
		}

		_days = CreateGrid(6);
		_dotsView = new GraphicsView { Drawable = _dots, InputTransparent = true, ZIndex = 1 };
		_days.Add(_dotsView, 0, 0);
		Grid.SetColumnSpan(_dotsView, 7);
		Grid.SetRowSpan(_dotsView, 6);
		if (Application.Current != null) Application.Current.RequestedThemeChanged += (_, _) => _dotsView.Invalidate();
		_dayView = new VerticalStackLayout { Children = { weekdays, _days } };
		_listView = new ScrollView { HeightRequest = BodyHeight };
		_body = new ContentView { HeightRequest = BodyHeight, Content = _dayView };

		var panel = new Border
		{
			Padding = 12,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.15f, Radius = 6, Offset = new Point(0, 2) },
			Content = new VerticalStackLayout { Children = { header, _body } },
		};
		panel.SetAppThemeColor(BackgroundColorProperty, Token("FtWhite"), Token("FtWhiteNight"));
		panel.GestureRecognizers.Add(new TapGestureRecognizer());

		Content = panel;
	}

	public void Prewarm()
	{
		if (_cellsBuilt >= 42) return;
		AddCells(7);
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(32), Prewarm);
	}

	private void AddCells(int count)
	{
		for (var end = Math.Min(42, _cellsBuilt + count); _cellsBuilt < end; _cellsBuilt++)
		{
			_days.Add(CreateDayCell(_cellsBuilt), _cellsBuilt % 7, _cellsBuilt / 7);
		}
	}

	public void Open(DateTime selected)
	{
		AddCells(42);
		_pending = selected.Date;
		_displayMonth = new DateTime(_pending.Year, _pending.Month, 1);
		_mode = PickerMode.Days;
		Render();
	}

	private void ShiftMonths(int months)
	{
		_displayMonth = _displayMonth.AddMonths(months);
		Render();
	}

	private void ToggleMode(PickerMode mode)
	{
		_mode = _mode == mode ? PickerMode.Days : mode;
		Render();
	}

	private void Render()
	{
		_monthButton.Text = _displayMonth.ToString("MMM", CultureInfo.InvariantCulture);
		_yearButton.Text = _displayMonth.ToString("yyyy", CultureInfo.InvariantCulture);

		if (_mode == PickerMode.Days)
		{
			_body.Content = _dayView;
			RenderDays();
		}
		else
		{
			_body.Content = _listView;
			RenderList();
		}
	}

	private void RenderDays()
	{
		var first = _displayMonth.AddDays(-(int)_displayMonth.DayOfWeek);
		var today = DateTime.Today;

		for (var i = 0; i < 42; i++)
		{
			var date = first.AddDays(i);
			_dayDates[i] = date;
			UpdateDayCell(i, date, date.Month == _displayMonth.Month, date == _pending, date == today, HasLogs?.Invoke(date) == true);
		}
		_dotsView.Invalidate();
	}

	sealed record PeriodRow(int Value, Grid Row, Label Check, Label Label);

	private void RenderList()
	{
		List<PeriodRow> rows;
		int selectedValue;

		if (_mode == PickerMode.Months)
		{
			if (_monthRows.Count == 0)
			{
				for (var m = 1; m <= 12; m++)
				{
					var month = m;
					var name = new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
					var row = CreatePeriodRow(month, name, () => PickPeriod(new DateTime(_displayMonth.Year, month, 1)));
					_monthRows.Add(row);
					_monthList.Add(row.Row);
				}
			}
			_listView.Content = _monthList;
			rows = _monthRows;
			selectedValue = _displayMonth.Month;
		}
		else
		{
			var today = DateTime.Today;
			var range = (From: Math.Min(_displayMonth.Year, today.Year - 10), To: Math.Max(_displayMonth.Year, today.Year));
			if (range != _yearRange)
			{
				_yearRange = range;
				_yearRows.Clear();
				_yearList.Clear();
				for (var y = range.To; y >= range.From; y--)
				{
					var year = y;
					var row = CreatePeriodRow(year, year.ToString(CultureInfo.InvariantCulture), () => PickPeriod(new DateTime(year, _displayMonth.Month, 1)));
					_yearRows.Add(row);
					_yearList.Add(row.Row);
				}
			}
			_listView.Content = _yearList;
			rows = _yearRows;
			selectedValue = _displayMonth.Year;
		}

		View? selectedRow = null;
		foreach (var row in rows)
		{
			var selected = row.Value == selectedValue;
			MarkPeriodRow(row, selected);
			if (selected) selectedRow = row.Row;
		}

		if (selectedRow != null)
		{
			Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), async () => await _listView.ScrollToAsync(selectedRow, ScrollToPosition.Center, false));
		}
	}

	private void PickPeriod(DateTime month)
	{
		_displayMonth = month;
		_mode = PickerMode.Days;
		Render();
	}

	private static PeriodRow CreatePeriodRow(int value, string text, Action action)
	{
		var check = new Label
		{
			FontFamily = FaIcons.FontFamily,
			Text = FaIcons.Check,
			FontSize = 20,
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center,
			InputTransparent = true,
		};
		check.SetAppThemeColor(Label.TextColorProperty, Token("FtPrimaryDark"), Token("FtPrimaryDarkNight"));
		var label = new Label
		{
			Text = text,
			Style = (Style)Application.Current!.Resources["FtBodyLarge"],
			VerticalTextAlignment = TextAlignment.Center,
			InputTransparent = true,
		};

		var touch = new Button
		{
			Style = (Style)Application.Current!.Resources["FtTextButton"],
			CornerRadius = 0,
			Padding = 0,
			Margin = new Thickness(-12, 0),
			HeightRequest = ListRowHeight,
			MinimumHeightRequest = ListRowHeight,
		};
		SemanticProperties.SetDescription(touch, text);
		touch.Clicked += (_, _) => action();

		var row = new Grid
		{
			HeightRequest = ListRowHeight,
			Padding = new Thickness(12, 0),
			ColumnSpacing = 12,
			ColumnDefinitions = { new ColumnDefinition(24), new ColumnDefinition(GridLength.Star) },
		};
		row.Add(touch, 0, 0);
		Grid.SetColumnSpan(touch, 2);
		row.Add(check, 0, 0);
		row.Add(label, 1, 0);
		return new PeriodRow(value, row, check, label);
	}

	private static void MarkPeriodRow(PeriodRow row, bool selected)
	{
		row.Check.IsVisible = selected;
		if (selected)
		{
			row.Row.SetAppThemeColor(BackgroundColorProperty, Token("FtPrimaryContainer"), Token("FtPrimaryContainerNight"));
			row.Label.SetAppThemeColor(Label.TextColorProperty, Token("FtPrimaryDark"), Token("FtPrimaryDarkNight"));
		}
		else
		{
			row.Row.BackgroundColor = Colors.Transparent;
			row.Label.SetAppThemeColor(Label.TextColorProperty, Token("FtTextSecondary"), Token("FtTextSecondaryNight"));
		}
	}

	private View CreateDayCell(int index)
	{
		var cell = new Button
		{
			BackgroundColor = Colors.Transparent,
			BorderWidth = 0,
			TextTransform = TextTransform.None,
			FontFamily = "RobotoRegular",
			FontSize = 14,
			CharacterSpacing = 0,
			Padding = 0,
			WidthRequest = CellSize,
			HeightRequest = CellSize,
			MinimumWidthRequest = CellSize,
			MinimumHeightRequest = CellSize,
			CornerRadius = (int)(CellSize / 2),
		};
		cell.SetAppThemeColor(Button.BorderColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
		cell.HorizontalOptions = LayoutOptions.Center;
		cell.Clicked += (_, _) => OnDayClicked(_dayDates[index]);

		_dayButtons[index] = cell;
		return cell;
	}

	private void OnDayClicked(DateTime date)
	{
		_pending = date;
		if (date.Month != _displayMonth.Month || date.Year != _displayMonth.Year) _displayMonth = new DateTime(date.Year, date.Month, 1);
		Render();
		Dispatcher.Dispatch(() => DateSelected?.Invoke(this, date));
	}

	private void UpdateDayCell(int index, DateTime date, bool inMonth, bool selected, bool today, bool hasLogs)
	{
		var cell = _dayButtons[index];

		cell.Text = date.Day.ToString(CultureInfo.InvariantCulture);
		SemanticProperties.SetDescription(cell, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + (hasLogs ? ", has logs" : ""));
		cell.BorderWidth = !selected && inMonth && today ? 1 : 0;

		if (selected)
		{
			cell.SetAppThemeColor(Button.BackgroundColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
		}
		else
		{
			cell.BackgroundColor = Colors.Transparent;
		}

		var (text, textNight) = selected ? ("FtWhite", "FtWhiteNight")
			: !inMonth ? ("FtTextDisabled", "FtTextDisabledNight")
			: today ? ("FtPrimary", "FtPrimaryNight")
			: ("FtTextPrimary", "FtTextPrimaryNight");
		cell.SetAppThemeColor(Button.TextColorProperty, Token(text), Token(textNight));
		_dots.Keys[index] = !hasLogs ? null : selected ? "FtWhite" : !inMonth ? "FtTextDisabled" : "FtPrimary";
	}

	private sealed class DayDots : IDrawable
	{
		public string?[] Keys { get; } = new string?[42];

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
			var cellWidth = (dirtyRect.Width - 6 * (float)CellGap) / 7;
			for (var i = 0; i < Keys.Length; i++)
			{
				var key = Keys[i];
				if (key == null) continue;
				var x = (i % 7) * (cellWidth + (float)CellGap) + cellWidth / 2;
				var y = (i / 7) * (float)(CellSize + CellGap) + (float)CellSize - 8;
				canvas.FillColor = Token(dark ? key + "Night" : key);
				canvas.FillCircle(x, y, 2);
			}
		}
	}

	private static Grid CreateGrid(int rows)
	{
		var grid = new Grid { ColumnSpacing = CellGap, RowSpacing = CellGap };
		for (var c = 0; c < 7; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		for (var r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(CellSize));
		return grid;
	}

	private static Button PeriodButton(Action action, string description)
	{
		var caret = new FontImageSource { FontFamily = FaIcons.FontFamily, Glyph = FaIcons.CaretDown, Size = 10 };
		caret.SetAppThemeColor(FontImageSource.ColorProperty, Token("FtIconDefault"), Token("FtIconDefaultNight"));

		var button = new Button
		{
			Style = (Style)Application.Current!.Resources["FtTextButton"],
			FontFamily = "RobotoRegular",
			FontSize = 14,
			CharacterSpacing = 0.114,
			Padding = new Thickness(8, 0),
			HeightRequest = 32,
			MinimumHeightRequest = 32,
			CornerRadius = 16,
			ImageSource = caret,
			ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right, 6),
		};
		button.SetAppThemeColor(Button.TextColorProperty, Token("FtTextPrimary"), Token("FtTextPrimaryNight"));
		SemanticProperties.SetDescription(button, description);
		button.Clicked += (_, _) => action();
		return button;
	}

	private static View Stepper(Button period, Action previous, Action next, string unit)
	{
		return new HorizontalStackLayout
		{
			Spacing = 4,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				ArrowButton(FaIcons.ChevronLeft, previous, $"Previous {unit}"),
				period,
				ArrowButton(FaIcons.ChevronRight, next, $"Next {unit}"),
			},
		};
	}

	private static Button ArrowButton(string glyph, Action action, string description)
	{
		var button = new Button
		{
			Text = glyph,
			Style = (Style)Application.Current!.Resources["FtTextButton"],
			FontFamily = FaIcons.FontFamily,
			FontSize = 14,
			CharacterSpacing = 0,
			Padding = 0,
			WidthRequest = 32,
			HeightRequest = 32,
			MinimumWidthRequest = 32,
			MinimumHeightRequest = 32,
			CornerRadius = 16,
		};
		button.SetAppThemeColor(Button.TextColorProperty, Token("FtIconDefault"), Token("FtIconDefaultNight"));
		SemanticProperties.SetDescription(button, description);
		button.Clicked += (_, _) => action();
		return button;
	}

	private static Color Token(string key) => (Color)Application.Current!.Resources[key];
}
