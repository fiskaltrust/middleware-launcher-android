using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
				fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
				fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
				fonts.AddFont("FontAwesome7Free-Solid-900.otf", "FontAwesomeSolid");
			})
			.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(Shell), typeof(FtShellRenderer));
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
