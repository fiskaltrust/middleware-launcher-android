namespace fiskaltrust.AndroidLauncher.Controls;

public partial class AppBar : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(AppBar), string.Empty);

    public static readonly BindableProperty TrailingGlyphProperty =
        BindableProperty.Create(nameof(TrailingGlyph), typeof(string), typeof(AppBar), null,
            propertyChanged: (bindable, _, newValue) =>
                ((AppBar)bindable).TrailingButton.IsVisible = !string.IsNullOrEmpty((string?)newValue));

    public static readonly BindableProperty TrailingDescriptionProperty =
        BindableProperty.Create(nameof(TrailingDescription), typeof(string), typeof(AppBar), null);

    public event EventHandler? TrailingTapped;

    public AppBar()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? TrailingGlyph
    {
        get => (string?)GetValue(TrailingGlyphProperty);
        set => SetValue(TrailingGlyphProperty, value);
    }

    public string? TrailingDescription
    {
        get => (string?)GetValue(TrailingDescriptionProperty);
        set => SetValue(TrailingDescriptionProperty, value);
    }

    public View TrailingAnchor => TrailingButton;

    private void OnTrailingTapped(object? sender, EventArgs e)
    {
        TrailingTapped?.Invoke(this, EventArgs.Empty);
    }
}
