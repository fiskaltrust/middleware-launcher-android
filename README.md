# fiskaltrust Android Launcher
The **fiskaltrust Android Launcher** is an application that hosts the packages of the **fiskaltrust Middleware** on Android, a modular fiscalization and POS platform that can be embedded into POS systems to suffice international fiscalization regulations.

For samples on how to **implement and roll-out** this App, please refer to our [documentation platform](https://docs.fiskaltrust.cloud) the [Android demo repository](https://github.com/fiskaltrust/middleware-demo-android).

## Overview
The Launcher for Android is - similar to our [Desktop Launcher](https://github.com/fiskaltrust/middleware-launcher) for Windows, Linux and macOS - responsible for hosting our dynamic Middleware packages on mobile devices running the Android operating system (version 7+). 

It hosts the same public, international [IPOS](https://github.com/fiskaltrust/middleware-interface-dotnet) interface as the Desktop Launcher, either via gRPC or REST, which means that multi-platform POS systems with existing Middleware implementations can easily embed it into their existing workflows.

Unlike the Desktop Launcher, where all supported communication protocols are supported by a single application, we've split the Android version into different Apps to reduce their respective size.

## Getting started
There are two ways of using the fiskaltrust.Middleware on Android:
1. By using the App directly from Google's Play store ([gRPC](https://play.google.com/store/apps/details?id=eu.fiskaltrust.androidlauncher.grpc) | [HTTP](https://play.google.com/store/apps/details?id=eu.fiskaltrust.androidlauncher.http))
2. By downloading the APK files from the _Cashbox_ section in our management portal, and installing them on the devices (ideally via device management). Please note that the download option is only available for cashboxes that contain supported packages and communication protocols only (as described under _Features and Limitations_ below).

## Implementation details & architecture
The `fiskaltrust.AndroidLauncher` App consists of a foreground service runnint the fiskaltrust.Middleware that is started or stopped from POS System Apps via **intents**. This allows the POS software to maintain full control about the fiscalization dependencies, and reduces the signing time when e.g. using hardware TSEs that can take up to 45 seconds to initialize (a delay that would not be acceptable for each receipt).

In both our internal tests and on-site at installations, the foreground service has been proven to be very energy-efficient in standby, not consuming notable battery amounts.

## How to use it
Download and install the _.APK_ on the Portal's Cashbox page or via the Play Store, as described above. 

A working internet connection is required at least during the first start of the Middleware, as it automatically downloads the configuration from our background services. Later, an internet connection is not required anymore (except when using a cloud TSE, of course). When using one of our archiving add-ons, we still recommend keeping the device connected to make use of our automatic cloud storage.

All details about our IPOS interface and how to use it for different business cases can be found in our [middleware documentation](https://docs.fiskaltrust.cloud/docs/poscreators/get-started).

## Features and Limitations
The current project offers very similar functionality to the Middleware's Desktop Launcher. It works with different SCUs, pulls the configuration from our Cloud services, and also reccurently uploads the processed receipts to fiskaltrust's revision-safe storage.

However, there are some limitations due to the used platform:
- Unlike the Desktop Launcher, the Android launcher **cannot** dynamically download packages at runtime, since this is not allowed due to Google's Play Store guidelines. Hence, the SCU and Queue packages are hardwired to the App, which must be updated to get new versions.
- We currently only support the _SQLite queue_ (which is sufficient for the use cases we heard of until now), and two SCUs: _fiskaly_ (cloud) and _Swissbit_ (hardware). The latter can be plugged into the device as an SD card. _If a cashbox is used that includes other Queues or SCUs, an exception is thrown_. If you need support for additional SCUs, we'd be happy to add it - please reach out to us in that case!

### Access Restrictions to SD Card Root Directory in Android

In recent Android versions, applications are restricted from accessing the root directory of SD cards. This security measure limits the potential risks associated with unauthorized access to critical system files and user data.

### Android and Swissbit

#### Swissbit Support for Additional.DAT Files

Swissbit supports additional .DAT files, and they are automatically created by putting a ".swissbitworm" file into any folder of the TSE. 
The creation of .DAT files is exclusively handled by the TSE during the startup process. To ensure the proper generation of these files, the TSE needs to be remounted or the device must be restarted. Failure to do so may result in a remount error, preventing the successful creation of .DAT files.


## Contributing
We welcome all kinds of contributions and feedback, e.g. via issues or pull requests, and want to thank every future contributors in advance!

Please check out the [contribution guidelines](https://github.com/fiskaltrust/.github/blob/main/CONTRIBUTING.md) for more detailed information about how to proceed.

## License
The fiskaltrust Middleware is released under the [EUPL 1.2](./LICENSE). 

As a Compliance-as-a-Service provider, the security and authenticity of the products installed on our users' endpoints is essential to us. To ensure that only peer-reviewed binaries are distributed by maintainers, fiskaltrust explicitly reserves the sole right to use the brand name "fiskaltrust Middleware" (and the brand names of related products and services) for the software provided here as open source - regardless of the spelling or abbreviation, as long as conclusions can be drawn about the original product name.  

The fiskaltrust Middleware (and related products and services) as contained in these repositories may therefore only be used in the form of binaries signed by fiskaltrust. 
