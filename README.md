# fiskaltrust Android Launcher
The **fiskaltrust Android Launcher** is an application that hosts the packages of the **fiskaltrust Middleware** on Android, a modular fiscalization and POS platform that can be embedded into POS systems to suffice international fiscalization regulations.

For samples on how to **implement and roll-out** this App, please refer to our [documentation platform](https://docs.fiskaltrust.cloud) the [Android demo repository](https://github.com/fiskaltrust/middleware-demo-android).

## Overview
The Launcher for Android is - similar to our [Desktop Launcher](https://github.com/fiskaltrust/middleware-launcher) for Windows, Linux and macOS - responsible for hosting our dynamic Middleware packages on mobile devices running the Android operating system (version 7+). 

It exposes the same public, international [IPOS](https://github.com/fiskaltrust/middleware-interface-dotnet) interface as the Desktop Launcher via the **PosSystemAPI**, which POS System Apps call directly through Android IPC (Intents or Messenger) - no network configuration is required.

## Getting started
To use the fiskaltrust.Middleware on Android, download the APK file from the [releases section](https://github.com/fiskaltrust/middleware-launcher-android/releases) of this repository and install it on the devices.

## Implementation details & architecture
The `fiskaltrust.AndroidLauncher` App consists of a foreground service running the fiskaltrust.Middleware. POS System Apps communicate with it directly via the **PosSystemAPI**, either through Android **Intents** or a bound **Messenger** service. This allows the POS software to maintain full control about the fiscalization dependencies, and reduces the signing time when e.g. using hardware TSEs that can take up to 45 seconds to initialize (a delay that would not be acceptable for each receipt).

In both our internal tests and on-site at installations, the foreground service has been proven to be very energy-efficient in standby, not consuming notable battery amounts.

## How to use it
Download and install the _.APK_ from the GitHub releases section, as described above. 

A working internet connection is required at least during the first start of the Middleware, as it automatically downloads the configuration from our background services. Later, an internet connection is not required anymore (except when using a cloud TSE, of course). When using one of our archiving add-ons, we still recommend keeping the device connected to make use of our automatic cloud storage.

### Communication Protocols

The Android Launcher supports multiple ways to communicate with the Middleware:

1. **Intent-based PosSystemAPI**: Use Android Intents to call the PosSystemAPI directly from your Android app, enabling offline fiscalization without network configuration. 
2. **Messenger-based PosSystemAPI**: Use Android Messenger to call the PosSystemAPI directly from your Android app, enabling offline fiscalization without network configuration.

All details about our IPOS interface and how to use it for different business cases can be found in our [documentation](https://docs.fiskaltrust.eu/docs/poscreators/possystem-api/android-intent).

## Features and Limitations
The current project offers very similar functionality to the Middleware's Desktop Launcher. It works with different SCUs, pulls the configuration from our Cloud services, and also recurrently uploads the processed receipts to fiskaltrust's revision-safe storage.

However, there are some limitations due to the used platform:
- Unlike the Desktop Launcher, the Android launcher **cannot** dynamically download packages at runtime, since this is not allowed due to Google's Play Store guidelines. Hence, the SCU and Queue packages are hardwired to the App, which must be updated to get new versions.
- We currently only support the _SQLite queue_ (which is sufficient for the use cases we heard of until now), and two SCUs: _fiskaly_ (cloud) and _Swissbit_ (hardware). The latter can be plugged into the device as an SD card. _If a cashbox is used that includes other Queues or SCUs, an exception is thrown_. If you need support for additional SCUs, we'd be happy to add it - please reach out to us in that case!

### Access Restrictions to SD Card Root Directory in Android

In recent Android versions, applications are restricted from accessing the root directory of SD cards. This security measure limits the potential risks associated with unauthorized access to critical system files and user data.

### Android and Swissbit

#### Swissbit Support for Additional.DAT Files

Swissbit supports additional .DAT files, and they are automatically created by putting a ".swissbitworm" file into any folder of the TSE. 
The creation of .DAT files is exclusively handled by the TSE during the startup process. To ensure the proper generation of these files, the TSE needs to be remounted or the device must be restarted. Failure to do so may result in a remount error, preventing the successful creation of .DAT files.

## Smoke Tests

End-to-end tests covering the PosSystemAPI (echo + sign receipt) through both integration surfaces: the Activity (intent-based) and the bound Service (Messenger-based). The whole test suite lives in a small standalone app, [`fiskaltrust.AndroidLauncher.SmokeTests`](test/fiskaltrust.AndroidLauncher.SmokeTests), which acts as a real third-party POS client - it binds to the Service / starts the Activity from its own process. The suite is run on the device via `adb shell am instrument`; no host-side test runner is involved.

### Requirements

- .NET 10 SDK
- JDK 21 (`JAVA_HOME` must be set, or discoverable by the .NET Android build)
- `adb` on `PATH` (part of Android SDK / Android Studio)
- Android emulator (API 24+, x86\_64) or a connected physical device

### Before running

Build and install both apps onto the emulator/device:
```sh
dotnet build src/fiskaltrust.AndroidLauncher/fiskaltrust.AndroidLauncher.csproj -f net10.0-android -t:Install
dotnet build test/fiskaltrust.AndroidLauncher.SmokeTests/fiskaltrust.AndroidLauncher.SmokeTests.csproj -f net10.0-android -t:Install
```

The launcher must be exempted from battery optimizations so it can run in the background either from the system "ignore battery optimizations" dialog or by granting the exemption via adb:
```sh
adb shell dumpsys deviceidle whitelist +eu.fiskaltrust.androidlauncher
```

### Running

```sh
adb shell am instrument -w eu.fiskaltrust.androidlauncher.smoketests/eu.fiskaltrust.androidlauncher.smoketests.TestInstrumentation
```


### Credentials

Default sandbox credentials are defined in [`test/fiskaltrust.AndroidLauncher.SmokeTests/TestConstants.cs`](test/fiskaltrust.AndroidLauncher.SmokeTests/TestConstants.cs). Override them via instrumentation arguments if needed:

| Argument      | Description   |
|---------------|---------------|
| `-e CashboxId <guid>`     | Cashbox GUID  |
| `-e AccessToken <token>`  | Access token  |

## Contributing
We welcome all kinds of contributions and feedback, e.g. via issues or pull requests, and want to thank every future contributors in advance!

Please check out the [contribution guidelines](https://github.com/fiskaltrust/.github/blob/main/CONTRIBUTING.md) for more detailed information about how to proceed.

## License
The fiskaltrust Middleware is released under the [EUPL 1.2](./LICENSE). 

As a Compliance-as-a-Service provider, the security and authenticity of the products installed on our users' endpoints is essential to us. To ensure that only peer-reviewed binaries are distributed by maintainers, fiskaltrust explicitly reserves the sole right to use the brand name "fiskaltrust Middleware" (and the brand names of related products and services) for the software provided here as open source - regardless of the spelling or abbreviation, as long as conclusions can be drawn about the original product name.  

The fiskaltrust Middleware (and related products and services) as contained in these repositories may therefore only be used in the form of binaries signed by fiskaltrust.
