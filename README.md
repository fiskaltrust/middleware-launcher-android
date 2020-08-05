# fiskaltrust Android Launcher
A prototype project that shows how the fiskaltrust Launcher can be included into Android, using Xamarin.

## Overview and Architecture
Right now, this example consists of two projects: 
- **[fiskaltrust.AndroidLauncher](src/fiskaltrust.Launcher.Android)** is the actual Launcher that hosts the Queue and SCU packages via gRPC, similar to the Windows/Linux launcher.
- **[fiskaltrust.AndroidLauncher.SampleClient](src/fiskaltrust.Launcher.Android.SampleClient)** is a minimal demo application that illustrates how the Middleware can be used and invoked.

The ```fiskaltrust.AndroidLauncher``` library contains a foreground service that can be started or stopped. We implemented this as some TSEs (namely Swissbit) take quite some time to initialize because of the required self-testation, often up to one minute - a delay that is not practicable in most application scenarios. Hence it makes sense to keep the Launcher running in the background. 

## How to use it
1. Include the Xamarin library into your project (currently as a direct project reference, later via NuGet)
2. Create a custom `MiddlewareServiceConnection`, implementing the `IMiddlewareServiceConnection` and `IPOSProvider` interfaces. This class is used to get both the service status and the IPOS implementation from the Launcher (a sample implementation can be found [here](https://github.com/fiskaltrust/middleware-launcher-android/blob/master/src/fiskaltrust.Launcher.Android.SampleClient/MiddlewareServiceConnection.cs)). In our sample application, we use the state to display if the service is running, and to allow the users to start/stop it then.
3. Start the Launcher with the static `MiddlewareLauncherService.Start(IMiddlewareServiceConnection, cashboxId, accessToken)` method (as shown [here](https://github.com/fiskaltrust/middleware-launcher-android/blob/master/src/fiskaltrust.Launcher.Android.SampleClient/MainActivity.cs#L39)). Make sure that you create your own cashbox containing only some of the supported packages, e.g. in our [sandbox portal](https://portal-sandbox.fiskaltrust.cloud). The Launcher will automatically download the cashbox configuration depending on the provided ```cashboxId``` and ```accessToken``` values. Currently only the sandbox portal is supported.
4. Get an `IPOS` reference via `_serviceConnection.GetPOSAsync()`, and e.g. use it to [sign a ReceiptRequest](https://github.com/fiskaltrust/middleware-launcher-android/blob/master/src/fiskaltrust.Launcher.Android.SampleClient/MainActivity.cs#L114).

All details about our IPOS interface and how to use it for different business cases can be found in our [middleware documentation](https://docs.fiskaltrust.cloud/doc/interface-doc/doc/general/general.html).

## Features and Limitations
The current project offers very similar functionality to the Windows/Linux Launcher. It works with different SCUs, pulls configuration from our Cloud services, and also frequently uploads processed receipts to those for backup- and export functionalities.

However, there are some limitations:
- The Android launcher cannot dynamically download packages, like we would do it on Windows/Linux, since this is not allowed due to Google's Play Store guidelines. Hence, the SCU and Queue packages are hardwired to the library which must be updated to get new versions.
- We currently only support the _SQLite queue_ (which is sufficient for the use cases we heard of until now), and two SCUs: _fiskaly_ and _Swissbit_. The latter can be plugged into the device as an SD card. _If a cashbox is used that includes different Queues or SCUs, an exception is thrown_.
- The current implementation can only download cashbox configurations from the fiskaltrust sandbox environment.

## Outlook
Ultimately, we will publish the Android Launcher as a NuGet or AAR package so that it can be used directly in customer's apps. The current project-based approach is due to the prototype-nature of this project. Later, you will be able to download the binaries directly from our [Portal](https://portal.fiskaltrust.de) or [NuGet.org](https://nuget.org).
