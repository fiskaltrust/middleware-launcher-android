using Android.Content.Res;
using Android.Graphics.Drawables;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using Google.Android.Material.Shape;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace fiskaltrust.AndroidLauncher;

public class FtShellRenderer : ShellRenderer
{
	protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
		=> new FtBottomNavViewAppearanceTracker(this, shellItem);
}

public class FtBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
	public FtBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
		: base(shellContext, shellItem)
	{
	}

	public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
	{
		base.SetAppearance(bottomView, appearance);
		Apply(bottomView);
	}

	public override void ResetAppearance(BottomNavigationView bottomView)
	{
		base.ResetAppearance(bottomView);
		Apply(bottomView);
	}

	private static void Apply(BottomNavigationView view)
	{
		var density = view.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
		int Dp(double value) => (int)Math.Round(value * density);

		var primary = Token("FtPrimary");
		var primaryContainer = Token("FtPrimaryContainer");
		var surface = Token("FtWhite");
		var border = Token("FtBorder");

		view.LabelVisibilityMode = NavigationBarView.LabelVisibilityLabeled;
		view.ItemIconSize = Dp(20);
		view.ItemActiveIndicatorEnabled = true;
		view.ItemActiveIndicatorWidth = Dp(56);
		view.ItemActiveIndicatorHeight = Dp(32);
		view.ItemActiveIndicatorColor = ColorStateList.ValueOf(primaryContainer);
		view.ItemActiveIndicatorShapeAppearance = ShapeAppearanceModel.InvokeBuilder()
			.SetAllCorners(CornerFamily.Rounded, Dp(16))
			.Build();
		view.ItemRippleColor = ColorStateList.ValueOf(Android.Graphics.Color.Argb(0x1F, primary.R, primary.G, primary.B));
		view.ItemPaddingTop = Dp(8);
		view.ItemPaddingBottom = Dp(12);
		view.ActiveIndicatorLabelPadding = Dp(8);
		view.ItemTextAppearanceActive = Resource.Style.FtNavLabel;
		view.ItemTextAppearanceInactive = Resource.Style.FtNavLabel;
		view.SetItemTextAppearanceActiveBoldEnabled(false);
		var itemColors = new ColorStateList(
			new[] { new[] { Android.Resource.Attribute.StateChecked }, Array.Empty<int>() },
			new int[] { primary, Token("FtNavInactive") });
		view.ItemTextColor = itemColors;
		view.ItemIconTintList = itemColors;

		var layers = new LayerDrawable(new Drawable[] { new ColorDrawable(border), new ColorDrawable(surface) });
		layers.SetLayerInset(1, 0, Dp(1), 0, 0);
		view.Background = layers;
		view.Elevation = 0;
	}

	private static Android.Graphics.Color Token(string key)
	{
		var app = Microsoft.Maui.Controls.Application.Current;
		var dark = app?.RequestedTheme == AppTheme.Dark;
		var name = dark ? key + "Night" : key;
		return app != null && app.Resources.TryGetValue(name, out var value) && value is Microsoft.Maui.Graphics.Color color
			? color.ToPlatform()
			: Android.Graphics.Color.Transparent;
	}
}
