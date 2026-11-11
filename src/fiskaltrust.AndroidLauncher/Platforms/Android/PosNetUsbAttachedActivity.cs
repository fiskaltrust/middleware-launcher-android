using Android.App;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;

namespace fiskaltrust.AndroidLauncher;

/// <summary>
/// PL only: receives the system's USB_DEVICE_ATTACHED intent for a PosNet printer, so accepting the prompt shown on
/// plugging it in grants the app the USB permission the SCU needs. Disabled by default, so other markets never see it;
/// <see cref="Signing.PLSSCDFactory"/> enables it when a cashbox uses a PosNet printer over USB (usbhost://).
/// </summary>
[Activity(Enabled = false, Exported = true, NoHistory = true, ExcludeFromRecents = true, Theme = "@android:style/Theme.NoDisplay")]
[IntentFilter([UsbManager.ActionUsbDeviceAttached])]
[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
[Register("eu.fiskaltrust.androidlauncher.PosNetUsbAttachedActivity")]
public class PosNetUsbAttachedActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Nothing to show: the permission comes with the prompt, and the SCU opens the printer on its next command.
        Finish();
    }
}
