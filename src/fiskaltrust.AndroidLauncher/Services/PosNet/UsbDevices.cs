using Android.Content;
using Android.Hardware.Usb;
using fiskaltrust.Middleware.SCU.PL.PosNet;

namespace fiskaltrust.AndroidLauncher.Services.PosNet;

internal static class UsbDevices
{
    public static UsbManager Manager(Context context)
        => context.GetSystemService(Context.UsbService) as UsbManager
            ?? throw new IOException("This Android device offers no USB host support.");

    public static UsbDevice? Find(UsbManager manager, PosNetDeviceAddress.UsbHost address)
        => manager.DeviceList?.Values.FirstOrDefault(device => device.VendorId == address.VendorId && device.ProductId == address.ProductId);
}
