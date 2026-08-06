using MauiIcons.Core;

namespace fiskaltrust.AndroidLauncher;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		_ = new MauiIcon();
	}

	public static event Action? Resumed;

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		window.Resumed += (_, _) => Resumed?.Invoke();
		return window;
	}
}