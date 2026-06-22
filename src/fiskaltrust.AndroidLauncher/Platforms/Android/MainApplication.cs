using Android.App;
using Android.Runtime;
using System.Runtime.InteropServices;
using fiskaltrust.AndroidLauncher;

namespace fiskaltrust.AndroidLauncher;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		try
		{
			NativeLibrary.SetDllImportResolver(
				typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly,
				(name, _, _) =>
				{
					if (name != "e_sqlite3") return IntPtr.Zero;
					return NativeLibrary.TryLoad("libsqlite.so", out var handle) ? handle : IntPtr.Zero;
				});
		}
		catch { }
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
