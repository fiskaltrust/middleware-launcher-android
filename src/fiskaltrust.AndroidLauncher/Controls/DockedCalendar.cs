using System.Globalization;
using fiskaltrust.AndroidLauncher.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace fiskaltrust.AndroidLauncher.Controls;

public class DockedCalendar : ContentView
{
	const double CellSize = 40;
	const double CellGap = 4;

	readonly Label _monthLabel;
	readonly Label _yearLabel;
	readonly Grid _days;
	DateTime _displayMonth;
	DateTime _pending;

	public event EventHandler<DateTime>? Confirmed;
	public event EventHandler? Cancelled;

	public DockedCalendar()
	{
		_monthLabel = PeriodLabel();
		_yearLabel = PeriodLabel();

		var header = new Grid
		{
			HeightRequest = 48,
			ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
		};
		header.Add(Stepper(_monthLabel, () => ShiftMonths(-1), () => ShiftMonths(1), "month"), 0, 0);
		header.Add(Stepper(_yearLabel, () => ShiftMonths(-12), () => ShiftMonths(12), "year"), 1, 0);

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
			Content = new VerticalStackLayout { Children = { header, weekdays, _days, actions } },
		};
		panel.SetAppThemeColor(BackgroundColorProperty, Token("FtWhite"), Token("FtWhiteNight"));
		panel.GestureRecognizers.Add(new TapGestureRecognizer());

		Content = panel;
	}

	public void Open(DateTime selected)
	{
		_pending = selected.Date;
		_displayMonth = new DateTime(_pending.Year, _pending.Month, 1);
		Render();
	}

	private void ShiftMonths(int months)
	{
		_displayMonth = _displayMonth.AddMonths(months);
		Render();
	}

	private void Render()
	{
		_monthLabel.Text = _displayMonth.ToString("MMM", CultureInfo.InvariantCulture);
		_yearLabel.Text = _displayMonth.ToString("yyyy", CultureInfo.InvariantCulture);

		_days.Clear();
		var first = _displayMonth.AddDays(-(int)_displayMonth.DayOfWeek);
		var today = DateTime.Today;

		for (var i = 0; i < 42; i++)
		{
			var date = first.AddDays(i);
			_days.Add(DayCell(date, date.Month == _displayMonth.Month, date == _pending, date == today), i % 7, i / 7);
		}
	}

	private Button DayCell(DateTime date, bool inMonth, bool selected, bool today)
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
		SemanticProperties.SetDescription(cell, date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));

		if (selected)
		{
			cell.SetAppThemeColor(Button.BackgroundColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtWhite"), Token("FtWhiteNight"));
		}
		else if (!inMonth)
		{
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtTextDisabled"), Token("FtTextDisabledNight"));
		}
		else if (today)
		{
			cell.BorderWidth = 1;
			cell.SetAppThemeColor(Button.BorderColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtPrimary"), Token("FtPrimaryNight"));
		}
		else
		{
			cell.SetAppThemeColor(Button.TextColorProperty, Token("FtTextPrimary"), Token("FtTextPrimaryNight"));
		}

		cell.Clicked += (_, _) =>
		{
			_pending = date;
			if (!inMonth) _displayMonth = new DateTime(date.Year, date.Month, 1);
			Render();
		};
		return cell;
	}

	private static Grid CreateGrid(int rows)
	{
		var grid = new Grid { ColumnSpacing = CellGap, RowSpacing = CellGap };
		for (var c = 0; c < 7; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(CellSize));
		for (var r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(CellSize));
		return grid;
	}

	private static Label PeriodLabel()
	{
		var label = new Label
		{
			FontSize = 14,
			CharacterSpacing = 0.114,
			VerticalTextAlignment = TextAlignment.Center,
		};
		label.SetAppThemeColor(Label.TextColorProperty, Token("FtTextPrimary"), Token("FtTextPrimaryNight"));
		return label;
	}

	private static View Stepper(Label period, Action previous, Action next, string unit)
	{
		var caret = new Label
		{
			FontFamily = FaIcons.FontFamily,
			Text = FaIcons.CaretDown,
			FontSize = 10,
			VerticalTextAlignment = TextAlignment.Center,
		};
		caret.SetAppThemeColor(Label.TextColorProperty, Token("FtIconDefault"), Token("FtIconDefaultNight"));

		return new HorizontalStackLayout
		{
			Spacing = 4,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				ArrowButton(FaIcons.ChevronLeft, previous, $"Previous {unit}"),
				new HorizontalStackLayout { Spacing = 6, Padding = new Thickness(4, 0), Children = { period, caret } },
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
