---
name: android-device-test
description: Build or download the launcher APK, install it on a physical Android device over adb, and test the middleware end to end by sending receipt requests through the PosSystemAPI intent and reading the responses from logcat. Use when asked to deploy the launcher to a device, test a change on real hardware (e.g. the POSNET printer for Poland), print sample receipts, or collect request/response samples.
---

# Deploy the launcher to a device and test it

The launcher hosts the middleware (queue + SCU) on the device. A test is: install an APK, send
`/v2/sign` requests to the launcher's **PosSystemAPI activity** with `adb shell am start`, and read the
response from logcat. `scripts/send_receipt.py` does the sending and reading.

## 0. Ask for what you can't know

Before starting, get from the user (never commit them, never print the token back):

- **Device serial**: e.g. `192.168.1.188:45587` for wireless adb. The port changes after a reboot or
  when wireless debugging is toggled; if `adb connect` fails, ask for the new port (or `adb pair` code).
- **Cashbox id and access token** of a **sandbox** cashbox configured for the device's market/SCU.
- **Which APK**: a local build of the working tree, or the CI artifact of a PR/commit.

Export them for the scripts (PowerShell: `$env:ADB_SERIAL = '...'`; bash: `export ADB_SERIAL=...`):
`ADB_SERIAL`, `FT_CASHBOX_ID`, `FT_ACCESS_TOKEN`. On Git Bash for Windows also set
`MSYS_NO_PATHCONV=1` before calling `adb shell` directly, or paths like `/v2/sign` get mangled.

## 1. Connect

```
adb connect <serial>
adb -s <serial> shell getprop ro.product.cpu.abi      # armeabi-v7a -> android-arm, arm64-v8a -> android-arm64
adb -s <serial> shell dumpsys package eu.fiskaltrust.androidlauncher | grep versionName
```

## 2. Get an APK

**Local build** (needs the .NET SDK from `global.json`, the `android` workload, JDK 21, and initialised
submodules: `git submodule update --init --recursive`):

```
dotnet build src/fiskaltrust.AndroidLauncher/fiskaltrust.AndroidLauncher.csproj -c Release -f net10.0-android -p:RuntimeIdentifier=android-arm -p:AndroidPackageFormat=apk -v:minimal
```

The APK is `src/fiskaltrust.AndroidLauncher/bin/Release/net10.0-android/<rid>/eu.fiskaltrust.androidlauncher-Signed.apk`.
Pick the RuntimeIdentifier from the device's ABI. A build takes several minutes: run it in the background
and redirect the log to a file; check it with `grep -E "Error\(s\)|error " <log>`.

**CI artifact** (workflow "Android Build and Upload" runs on every PR):

```
gh run list --repo fiskaltrust/middleware-launcher-android --branch <branch> --workflow "Android Build and Upload" --limit 3
gh run download <run-id> --repo fiskaltrust/middleware-launcher-android -n artifacts-android -D <empty dir>
```

The CI APK's versionName ends in the PR merge commit (`-g<sha>`), not the branch head.

## 3. Install

```
adb -s <serial> install -r <apk>
adb -s <serial> shell dumpsys deviceidle whitelist +eu.fiskaltrust.androidlauncher
```

- **`INSTALL_FAILED_UPDATE_INCOMPATIBLE` (signatures do not match)**: local builds and CI builds are
  signed with different keys. Switching means `adb uninstall eu.fiskaltrust.androidlauncher`, which
  **wipes the app's data: the queue's SQLite journal and the cashbox configuration**. Ask the user
  before uninstalling. Prefer staying on one kind of build for a test series.
- After a fresh install the queue is not activated (responses carry `ftState ...0000_0001`): send the
  initial-operation receipt (`init` shortcut for PL) once.
- **USB devices** (e.g. a POSNET printer on USB): the first request makes Android show a USB permission
  dialog **on the device**. Ask the user to tap Allow. logcat shows
  `Access to the USB device usbhost://VVVV:PPPP is granted.` once it is done.

## 4. Send requests

```
python -I .claude/skills/android-device-test/scripts/send_receipt.py --out <results dir> zero
python -I .claude/skills/android-device-test/scripts/send_receipt.py --out <results dir> <file.json> <dir> ...
```

- Shortcuts (PL only): `zero` (status), `init` (initial operation), `daily` (daily closing).
- Files may hold `"ftCashBoxID": "{{ ftCashBoxID }}"`; the script fills it in, sets `cbReceiptMoment`
  to now (`--keep-moment` to keep it) and appends a run suffix to `cbReceiptReference` and
  `cbPreviousReceiptReference`, so a return that references a sale in the same run still matches.
- Results go to `<out>/<name>.json`; if that file exists, `<name>-2.json` and so on.
- Each result file holds `{request, response}` with the cashbox id replaced by the placeholder again
  in both, so results can be shared. Never put the access token into a file you share or commit.
- One line per request: `ftState`, `ok`/`ERROR` (`ftState & 0xFFFFFFFF == 0xEEEEEEEE`), the failure
  signatures and the fiscal numbers.
- Run long series in the background; each request waits up to `--timeout` seconds (default 240) for
  its response. Requests run one at a time: the queue processes them in order, so a zero receipt does
  not overtake a sale that is waiting for the printer.
- A response counts only if its `cbReceiptReference` matches the request. If a request times out (e.g.
  during an unplug test), the script stops instead of sending the rest: the request may still be pending
  in the queue. **Do not resend the timed-out file**: it would get a new reference and be a second
  sale, and both may print once the printer is back. Restore the printer, wait for the pending request
  to finish (a `zero` receipt queues behind it and returns once it is done), check on paper whether it
  was printed, then send only the requests after it.
- The response is read from logcat, which cuts entries at about 4 KB. A longer response is saved as
  `raw` and reported as `NO VALID RESPONSE`; the receipt was still processed, so do not resend it.

- **"The requested cashboxid or accesstoken do not match the currently running cashbox"**: the
  launcher keeps running the cashbox it was started with and refuses other credentials. Check the
  values first: on Windows, a value captured from a command's output can end in `\r`. To switch to
  (or restart) the cashbox in the env vars, send `/v2/echo` with body `{"Message":null}`; the launcher
  restarts the middleware with the request's credentials.

### Sample requests

- `samples/pl/`: POSNET (Poland) printouts — payment slips (`cbPayItemLines`, `Receipt`), `cbReceiptLines`,
  barcode, QR options, info lines, a GS1 Digital Link QR, and the validation errors (`ERR-*`, refused
  before anything is printed). They run against a POSNET SCU in a sandbox cashbox.
- Business cases (sales, discounts, storno, return, NIP, closings) live with the SCU's end-to-end tests:
  `middleware/scu-pl/test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest/BusinessCases/*/*.json`.
  Send the cash sale before the return: the return references it.

### Don't send without asking

- **Out of operation** (`...4002`): deactivates the queue for good.
- **Daily closing**: closes the day on the fiscal printer; on a fiscalised printer this is a real Z report.
- Anything against a **production** cashbox.

## 5. What Claude can't check: the paper

The response only says the device accepted the commands. Layout, alignment, cut, QR/barcode
readability are only visible on paper. After printing, tell the user which fiscal document numbers to
look at (from `Numer dokumentu fiskalnego`) and ask them to confirm or describe the problem. For codes,
ask them to scan them.

## 6. Tests that need the user's hands

For an unplug test (USB cable or printer power during a sale): start the request in the background,
tell the user exactly when to pull the cable (e.g. "as soon as paper moves"), then when to plug it back
in. The SCU waits without a time limit for an unconfirmed receipt, so the response comes only after the
printer is back. Then send `zero` and compare the printer's numbering with the response.

## 7. Logs

```
adb -s <serial> logcat -d -s PosSystemAPI                       # requests and responses
adb -s <serial> logcat -d | grep -a fiskaltrust.AndroidLauncher  # launcher / middleware log
adb -s <serial> logcat -d -s AndroidRuntime                      # crashes
```

## 8. Sharing results

Zip the results directory with a short README: APK (run id or commit, `versionName`), device and
printer (model, firmware, serial, fiscalised or not), date, a table of sample → `ftState` → fiscal
number/error, and what was not covered. Check the zip for the access token before sending it.
