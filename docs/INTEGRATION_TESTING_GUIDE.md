# PosSystemAPI Intent Integration Tests

This directory contains integration testing infrastructure for the Intent-based PosSystemAPI.

## Overview

The PosSystemAPI Intent interface allows Android applications to communicate with the fiskaltrust.Middleware through Android Intents. These integration tests verify that the Intent-based communication works correctly end-to-end.

## Architecture

The PosSystemAPI Activity routes requests based on the endpoint:
- **Local endpoints** (`/sign`, `/echo`, `/journal`): Routed to local middleware at `localhost:1200`
- **Cloud endpoints** (all others): Routed to fiskaltrust PosSystemAPI cloud service

This allows offline fiscalization for basic operations while enabling advanced cloud features like cart management, payment processing, and receipt issuance.

## Test Infrastructure

The test infrastructure is located in `test/fiskaltrust.AndroidLauncher.SmokeTests/PosSystemAPIIntentTestHelper.cs`.

### PosSystemAPIIntentTestHelper

A reusable helper class that simplifies testing Intent-based API calls. It:
- Handles Base64URL encoding/decoding automatically
- Manages the Intent creation and sending process
- Waits for and processes result Intents
- Provides a clean async API for testing

### Usage Example

```csharp
// In your test class (inheriting from Activity)
var testHelper = new PosSystemAPIIntentTestHelper(this);

// Send an Intent call
var headers = new Dictionary<string, string>
{
    { "x-cashbox-id", "your-cashbox-id" },
    { "x-cashbox-accesstoken", "your-token" },
    { "x-operation-id", Guid.NewGuid().ToString() }
};

var body = JsonConvert.SerializeObject(new { Message = "Test" });
var result = await testHelper.SendIntentAsync("POST", "/echo", headers, body);

// Verify result
Assert.AreEqual("200", result.StatusCode);

// Don't forget to wire up OnActivityResult
protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
{
    base.OnActivityResult(requestCode, resultCode, data);
    testHelper.OnActivityResult(requestCode, resultCode, data);
}
```;

var body = JsonConvert.SerializeObject(new { Message = "Test" });
var result = await testHelper.SendIntentAsync("POST", "/echo", headers, body);

// Verify result
Assert.AreEqual("200", result.StatusCode);

// Don't forget to wire up OnActivityResult
protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
{
    base.OnActivityResult(requestCode, resultCode, data);
    testHelper.OnActivityResult(requestCode, resultCode, data);
}
```

## Running Integration Tests

### Option 1: Using the DEBUG Test Activity

In DEBUG builds, a `PosSystemAPIIntentTestActivity` is available that demonstrates basic Intent API testing.

To use it:
1. Build the app in DEBUG mode
2. Deploy to a device/emulator with the middleware running
3. Use adb to start the test activity:
   ```bash
   adb shell am start -n eu.fiskaltrust.androidlauncher/eu.fiskaltrust.androidlauncher.Tests.PosSystemAPIIntentTestActivity
   ```
4. Check logcat for test results:
   ```bash
   adb logcat -s PosSystemAPIIntentTest:I PosSystemAPIIntentTest:E
   ```

### Option 2: Custom Integration Tests

You can create your own integration tests using the `PosSystemAPIIntentTestHelper`:

1. Create a test Activity in your test app
2. Instantiate `PosSystemAPIIntentTestHelper`
3. Call `SendIntentAsync()` to test different scenarios
4. Wire up `OnActivityResult()` to receive responses

### Option 3: Appium/UI Automation Tests

For automated testing in CI/CD pipelines, you can use Appium to:
1. Start the middleware via broadcast Intent
2. Use adb shell commands to send Intent calls
3. Read logcat output to verify behavior

Example:
```csharp
// Start middleware
_driver.ExecuteScript("mobile: shell", new Dictionary<string, object> {
    { "command", "am" },
    { "args", new[] { "broadcast", "-a", "...", "-n", "..." } }
});

// Send Intent API call
_driver.ExecuteScript("mobile: shell", new Dictionary<string, object> {
    { "command", "am" },
    { "args", new[] { 
        "start", 
        "-n", "eu.fiskaltrust.androidlauncher/eu.fiskaltrust.androidlauncher.PosSystemAPI",
        "--es", "Method", "POST",
        "--es", "Path", "/echo",
        "--es", "HeaderJsonObjectBase64Url", headersB64
    } }
});

// Check logcat for results
var logs = _driver.ExecuteScript("mobile: shell", new Dictionary<string, object> {
    { "command", "logcat" },
    { "args", new[] { "-d", "-s", "PosSystemAPI:I" } }
});
```

## Test Scenarios

The integration tests should cover:

### Basic Functionality
- ✅ Echo endpoint with valid credentials
- ✅ Sign endpoint with receipt data
- ✅ Pay endpoint with payment request

### Error Handling
- ✅ Missing required Intent extras (Method, Path, Headers)
- ✅ Invalid HTTP method
- ✅ Invalid Base64URL encoding
- ✅ Middleware not running (502 error expected)
- ✅ Invalid credentials (401 error expected)

### Edge Cases
- ✅ Special characters in request body (UTF-8, emojis)
- ✅ Large request bodies
- ✅ Empty request body for GET requests
- ✅ Concurrent Intent calls
- ✅ Intent call timeout scenarios

### Integration Scenarios
- ✅ Complete receipt fiscalization flow
- ✅ Retry with same operation ID (idempotency)
- ✅ Multiple sequential operations
- ✅ Error recovery and retry logic

## Prerequisites

To run integration tests:
1. Android device or emulator (API 24+)
2. fiskaltrust.Middleware installed and configured
3. Valid cashbox ID and access token
4. Middleware service running (started via broadcast Intent)

## Troubleshooting

### Intent not received
- Verify the middleware app is installed: `adb shell pm list packages | grep fiskaltrust`
- Check that PosSystemAPIActivity is registered: `adb shell dumpsys package eu.fiskaltrust.androidlauncher | grep Activity`
- Ensure you're using explicit Intents with correct package/class names

### No response from Intent
- Check if middleware is running: Look for notifications or use `adb shell ps | grep fiskaltrust`
- Verify middleware HTTP endpoint is accessible: `adb shell curl http://localhost:1200/echo`
- Check logcat for errors: `adb logcat -s PosSystemAPI:E`

### Base64URL encoding issues
- Ensure no padding characters (=) in encoded strings
- Verify + and / are replaced with - and _
- Test with the Base64UrlHelper unit tests first

## Future Enhancements

Possible improvements for the test infrastructure:
- [ ] Add Espresso-based instrumented tests
- [ ] Create sample test app demonstrating all endpoints
- [ ] Add performance/load testing scenarios
- [ ] Integrate with CI/CD pipeline
- [ ] Add test data generators for various markets (AT, DE, FR, etc.)
- [ ] Create mock middleware for offline testing
