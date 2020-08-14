# fiskaltrust Android Launcher
The fiskaltrust Android Launcher, which allows hosting fiskaltrust Middleware packages on mobile devices.

For samples on how to use this, please refer to the [Android demo repository](https://github.com/fiskaltrust/middleware-demo-android).

## Overview and Architecture
The `fiskaltrust.AndroidLauncher` App contains a foreground service that can be started or stopped from POS System Apps via Intents. The reason why implemented it this wasy is that some TSEs (namely Swissbit) take quite some time to initialize because of the required self-testation, often up to one minute - a delay that is not practicable in most application scenarios. Hence it makes sense to keep the Launcher running in the background. 

Our tests show that the foreground service does not consume much battery, so it should be safe to keep it open during business hours in most scenarios.

## How to use it
Download and install the _.APK_ on the Portal's Cashbox page. Please note that the download option is only available for cashboxes that contain supported packages and communication protocols only (as described under _Features and Limitations_ below).

A working internet connection is required at least during the first start of the Middleware, as it automatically downloads the configuration from our background services. Later, an internet connection is not required anymore (except when using a cloud TSE, of course). When using a carefree package, we still recommend keeping the device connected to make use of our automatic cloud backup.

All details about our IPOS interface and how to use it for different business cases can be found in our [middleware documentation](https://docs.fiskaltrust.cloud/doc/interface-doc/doc/general/general.html).

## Features and Limitations
The current project offers very similar functionality to the Windows/Linux Launcher. It works with different SCUs, pulls configuration from our Cloud services, and also frequently uploads processed receipts to those for backup- and export functionalities.

However, there are some limitations:
- The Android launcher cannot dynamically download packages, like we would do it on Windows/Linux, since this is not allowed due to Google's Play Store guidelines. Hence, the SCU and Queue packages are hardwired to the library which must be updated to get new versions.
- We currently only support the _SQLite queue_ (which is sufficient for the use cases we heard of until now), and two SCUs: _fiskaly_ and _Swissbit_. The latter can be plugged into the device as an SD card. _If a cashbox is used that includes other Queues or SCUs, an exception is thrown_.
