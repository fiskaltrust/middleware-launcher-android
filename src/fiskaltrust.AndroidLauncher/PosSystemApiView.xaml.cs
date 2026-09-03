namespace fiskaltrust.AndroidLauncher;

public partial class PosSystemApiView : ContentView
{
    public PosSystemApiView()
    {
        InitializeComponent();
    }

    public void SetEndpoint(string text) => endpointLabel.Text = text;

    public void SetStage(string text) => stageLabel.Text = text;
}
