using Android.App;
using Android.Content;

namespace fiskaltrust.AndroidLauncher;

public partial class ManageSpaceView : ContentView
{
    private readonly Activity _activity;

    public ManageSpaceView(Activity activity)
    {
        InitializeComponent();
        _activity = activity;
    }

    private void OnClearDataClicked(object sender, EventArgs e)
    {
        ((ActivityManager)_activity.GetSystemService(Context.ActivityService)!).ClearApplicationUserData();
        _activity.Finish();
    }
}
