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
	readonly VerticalStackLayout _list;
	readonly ScrollView _listView;
	readonly ContentView _body;
	DateTime _displayMonth;
	DateTime _pending;
	PickerMode _mode;

	public event EventHandler<DateTime>? Confirmed;
	public event EventHandler? Cancelled;

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
		_dayView = new VerticalStackLayout { Children = { weekdays, _days } };
		_list = new VerticalStackLayout();
		_listView = new ScrollView { HeightRequest = BodyHeight, Content = _list };
		_body = new ContentView { HeightRequest = BodyHeight, Content = _dayView };

		var cancel = ActionButton("Cancel", () => Cancelled?.Invoke(this, EventArgs.Empty));
		var ok = ActionButton("OK", () => Confirmed?.Invoke(this, _pending));
		var actions = new HorizontalStackLayout
		{
			Spacing = 8,
			Padding = new Thickness(0, 8, 0, 0),
			HorizontalOptions = LayoutOptions.End,
			Children = { cancel, ok },
		};

		var panel = new Border
		{
			WidthRequest = 328,
			Padding = 12,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.15f, Radius = 6, Offset = new Point(0, 2) },
			Content = new VerticalStackLayout { Children = { header, _body, actions } },
		};
		panel.SetAppThemeColor(BackgroundColorProperty, Token("FtWhite"), Token("FtWhiteNight"));
		panel.GestureRecognizers.Add(new TapGestureRecognizer());

		Content = panel;
	}

	public void Open(DateTime selected)
	{
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
		_days.Clear();
		var first = _displayMonth.AddDays(-(int)_displayMonth.DayOfWeek);
		var today = DateTime.Today;

		for (var i = 0; i < 42; i++)
		{
			var date = first.AddDays(i);
			_days.Add(DayCell(date, date.Month == _displayMonth.Month, date == _pending, date == today, HasLogs?.Invoke(date) == true), i % 7, i / 7);
		}
	}

	private void RenderList()
	{
		_list.Clear();
		View? selectedRow = null;

		if (_mode == PickerMode.Months)
		{
			for (var m = 1; m <= 12; m++)
			{
				var month = m;
				var selected = month == _displayMonth.Month;
				var name = new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
				var row = ListRow(name, selected, () => PickPeriod(new DateTime(_displayMonth.Year, month, 1)));
				_list.Add(row);
				if (selected) selectedRow = row;
			}
		}
		else
		{
			var today = DateTime.Today;
			var from = Math.Min(_displayMonth.Year, today.Year - 10);
			var to = Math.Max(_displayMonth.Year, today.Year);
			for (var y = to; y >= from; y--)
			{
				var year = y;
				var selected = year == _displayMonth.Year;
				var row = ListRow(year.ToString(CultureInfo.InvariantCulture), selected, () => PickPeriod(new DateTime(year, _displayMonth.Month, 1)));
				_list.Add(row);
				if (selected) selectedRow = row;
			}
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

	private static View ListRow(string text, bool selected, Action action)
	{
		var check = new Label
		{
			FontFamily = FaIcons.FontFamily,
			Text = selected ? FaIcons.Check : "",
			FontSize = 20,
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center,
			InputTransparent = true,
		};
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

		if (selected)
		{
			row.SetAppThemeColor(BackgroundColorProperty, Token("FtPrimaryContainer"), Token("FtPrimaryContainerNight"));
			check.SetAppThemeColor(Label.TextColorProperty, Token("FtPrimaryDark"), Token("FtPrimaryDarkNight"));
			label.SetAppThemeColor(Label.TextColorProperty, Token("FtPrimaryDark"), Token("FtPrimaryDarkNight"));
		}
		else
		{
			label.SetAppThemeColor(Label.TextColorProperty, Token("FtTextSecondary"), Token("FtTextSecondaryNight"));
		}

		row.Add(touch, 0, 0);
		Grid.SetColumnSpan(touch, 2);
		row.Add(check, 0, 0);
		row.Add(label, 1, 0);
		return row;
	}

	private View DayCell(DateTime date, bool inMonth, bool selected, bool today, bool hasLogs)
	{
		var cell = new Button
		{
			Text = date.Day.ToString(CultureInfo.InvariantCulture),
			Style = (Style)Application.Current!.Resources["FtTextButton"],
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
		SemanticProperties.SetDescription(cell, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + (hasLogs ? ", has logs" : ""));

		var dot = new Ellipse
		{
			WidthRequest = 4,
			HeightRequest = 4,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.End,
			Margin = new Thickness(0, 0, 0, 6),
			InputTransparent = true,
			IsVisible = hasLogs,
		};

		if (selected)
		{
			cell.SetAppThemeColor(Button.BackgroundColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtWhite"), Token("FtWhiteNight"));
			dot.SetAppThemeColor(Shape.FillProperty, Token("FtWhite"), Token("FtWhiteNight"));
		}
		else if (!inMonth)
		{
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtTextDisabled"), Token("FtTextDisabledNight"));
			dot.SetAppThemeColor(Shape.FillProperty, Token("FtTextDisabled"), Token("FtTextDisabledNight"));
		}
		else if (today)
		{
			cell.BorderWidth = 1;
			cell.SetAppThemeColor(Button.BorderColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
			dot.SetAppThemeColor(Shape.FillProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
		}
		else
		{
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtTextPrimary"), Token("FtTextPrimaryNight"));
			dot.SetAppThemeColor(Shape.FillProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
		}

		cell.Clicked += (_, _) =>
		{
			_pending = date;
			if (!inMonth) _displayMonth = new DateTime(date.Year, date.Month, 1);
			Render();
		};

		return new Grid { WidthRequest = CellSize, HeightRequest = CellSize, Children = { cell, dot } };
	}

	private static Grid CreateGrid(int rows)
	{
		var grid = new Grid { ColumnSpacing = CellGap, RowSpacing = CellGap };
		for (var c = 0; c < 7; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(CellSize));
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

	private static Button ActionButton(string text, Action action)
	{
		var button = new Button
		{
			Text = text,
			Style = (Style)Application.Current!.Resources["FtTextButton"],
			Padding = new Thickness(12, 0),
		};
		button.Clicked += (_, _) => action();
		return button;
	}

	private static Color Token(string key) => (Color)Application.Current!.Resources[key];
}
