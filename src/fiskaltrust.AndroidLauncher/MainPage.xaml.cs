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
        SetImageSource();
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

    private void SetImageSource()
    {
        if (ftLogo != null)
        {
            // Try multiple approaches to load the image
            try
            {
                // First try with extension
                ftLogo.Source = ImageSource.FromFile("ft_notification.png");
                System.Diagnostics.Debug.WriteLine("Set image source to: ft_notification.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load ft_notification.png: {ex.Message}");
                
                try
                {
                    // Try without extension
                    ftLogo.Source = ImageSource.FromFile("ft_notification");
                    System.Diagnostics.Debug.WriteLine("Set image source to: ft_notification");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load ft_notification: {ex2.Message}");
                    
                    // Try using appicon as fallback
                    try
                    {
                        ftLogo.Source = ImageSource.FromFile("appicon.png");
                        System.Diagnostics.Debug.WriteLine("Set image source to fallback: appicon.png");
                    }
                    catch (Exception ex3)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load fallback image: {ex3.Message}");
                    }
                }
            }
        }
    }
}

