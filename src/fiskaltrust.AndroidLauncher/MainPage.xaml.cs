using System.ComponentModel;

namespace fiskaltrust.AndroidLauncher;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private string _versionText = string.Empty;

    public string VersionText
    {
        get => _versionText;
        set
        {
            _versionText = value;
            OnPropertyChanged();
        }
    }

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        LoadVersionInfo();
    }

    private void LoadVersionInfo()
    {
        try
        {
            var version = AppInfo.Current.VersionString;
            var build = AppInfo.Current.BuildString;
            VersionText = $"Version {version} (Build {build})";
        }
        catch (Exception ex)
        {
            // Fallback if version info is not available
            VersionText = "Version information unavailable";
        }
    }
}

