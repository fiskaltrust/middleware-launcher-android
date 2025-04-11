using MauiIcons.Core;

namespace fiskaltrust.AndroidLauncher;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		_ = new MauiIcon();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}