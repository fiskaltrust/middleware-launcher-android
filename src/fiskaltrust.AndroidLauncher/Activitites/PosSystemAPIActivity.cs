using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Grpc;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Http.Broadcasting;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Apache.Http.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    /// <summary>
    /// Activity that handles Intent-based POS System API calls.
    /// Routes /sign and /echo to local middleware, other endpoints to cloud PosSystemAPI.
    /// </summary>
    [Activity(
        Label = "PosSystemAPI",
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPI",
        Enabled = true,
        Exported = true)]
    public class PosSystemAPIActivity : Activity
    {

        public static LocalMiddlewareLauncher? LocalMiddlewareServiceInstance { get; set; }

        // TaskCompletionSource to signal when the service is ready
        private static TaskCompletionSource<bool>? _serviceInitializationTcs;

        private const string TAG = "PosSystemAPI";

        // Local middleware endpoint for sign and echo
        private const string LOCALHOST_BASE_URL = "http://localhost:1200";

        // Intent extra keys for input
        private const string EXTRA_METHOD = "Method";
        private const string EXTRA_PATH = "Path";
        private const string EXTRA_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";
        private const string EXTRA_BODY_BASE64URL = "BodyBase64Url";

        // Intent extra keys for output
        private const string EXTRA_STATUS_CODE = "StatusCode";
        private const string EXTRA_CONTENT_BASE64URL = "ContentBase64Url";
        private const string EXTRA_CONTENT_TYPE_BASE64URL = "ContentTypeBase64Url";
        private const string EXTRA_RESPONSE_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";

        // Local endpoints that should be handled by the local middleware (defaults without version)
        private static readonly HashSet<string> LocalEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/sign",
            "/v2/sign",
            "/echo",
            "/v2/echo",
            "/journal",
            "/v2/journal",
        };

        // In-memory cache for operation results (idempotency for local requests)
        // Key: x-operation-id, Value: cached response data
        private static readonly ConcurrentDictionary<string, CachedOperationResult> OperationCache = new ConcurrentDictionary<string, CachedOperationResult>();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Log.Info(TAG, "PosSystemAPI Activity started");

            // Process the intent asynchronously
            Task.Run(async () => await ProcessIntentAsync());
        }

        private async Task ProcessIntentAsync()
        {
            try
            {
                var intent = Intent;
                if (intent == null)
                {
                    Log.Error(TAG, "Intent is null");
                    FinishWithError(500, "Intent is null");
                    return;
                }

                // Extract intent extras
                var method = intent.GetStringExtra(EXTRA_METHOD);
                var path = intent.GetStringExtra(EXTRA_PATH);
                var headerBase64Url = intent.GetStringExtra(EXTRA_HEADER_JSON_BASE64URL);
                var bodyBase64Url = intent.GetStringExtra(EXTRA_BODY_BASE64URL);

                Log.Info(TAG, $"Processing request: {method} {path}");

                // Validate required fields
                if (string.IsNullOrEmpty(method))
                {
                    Log.Error(TAG, "Method is missing");
                    FinishWithError(400, "Method is required");
                    return;
                }

                if (string.IsNullOrEmpty(path))
                {
                    Log.Error(TAG, "Path is missing");
                    FinishWithError(400, "Path is required");
                    return;
                }

                if (string.IsNullOrEmpty(headerBase64Url))
                {
                    Log.Error(TAG, "HeaderJsonObjectBase64Url is missing");
                    FinishWithError(400, "HeaderJsonObjectBase64Url is required");
                    return;
                }

                // Decode headers
                Dictionary<string, string> headers;
                try
                {
                    var headersJson = Base64UrlHelper.Decode(headerBase64Url);
                    headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson)
                        ?? new Dictionary<string, string>();
                    Log.Debug(TAG, $"Decoded {headers.Count} headers");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to decode headers: {ex.Message}");
                    FinishWithError(400, $"Invalid headers format: {ex.Message}");
                    return;
                }

                if (!headers.ContainsKey("x-operation-id"))
                {
                    FinishWithError(400, $"The required header x-operation-id was not sent.");
                    return;
                }

                // Decode body if present
                string? body = null;
                if (!string.IsNullOrEmpty(bodyBase64Url))
                {
                    try
                    {
                        body = Base64UrlHelper.Decode(bodyBase64Url);
                        Log.Debug(TAG, $"Decoded body: {body.Length} characters");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(TAG, $"Failed to decode body: {ex.Message}");
                        FinishWithError(400, $"Invalid body format: {ex.Message}");
                        return;
                    }
                }

                // Normalize path
                var normalizedPath = path.StartsWith("/") ? path : "/" + path;

                // Validate endpoint version - only support defaults (no version) and /v2
                if (IsUnsupportedVersion(normalizedPath))
                {
                    Log.Error(TAG, $"Unsupported endpoint version: {normalizedPath}");
                    FinishWithError(400, $"Unsupported endpoint version. Only default endpoints (e.g., /sign, /echo) and /v2/* endpoints are supported. Please do not use /v0/* or /v1/* versions.");
                    return;
                }

                // Determine if this is a local or cloud endpoint
                var isLocalEndpoint = IsLocalEndpoint(normalizedPath);

                var cashBoxId = Guid.Parse(headers.GetValueOrDefault("x-cashbox-id", Guid.Empty.ToString())!);
                var accessToken = headers.GetValueOrDefault("x-cashbox-accesstoken", string.Empty)!;

                if (isLocalEndpoint)
                {
                    Log.Info(TAG, $"Routing to local middleware: {normalizedPath}");
                    await MakeLocalRequestAsync(cashBoxId, accessToken, method, normalizedPath, headers, body);
                }
                else
                {
                    Log.Info(TAG, $"Routing to cloud PosSystemAPI: {normalizedPath}");
                    await MakeCloudRequestAsync(method, normalizedPath, headers, body);
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Unexpected error: {ex}");
                FinishWithError(500, $"Internal error: {ex.Message}");
            }
        }

        private bool IsUnsupportedVersion(string path)
        {
            // Check if path starts with unsupported version prefixes
            // We only support defaults (no version) and /v2
            // Reject /v0, /v1, /json/v0, /json/v1, /xml/v0, /xml/v1
            var unsupportedPrefixes = new[] { "/v0/", "/v1/", "/json/v0/", "/json/v1/", "/xml/v0/", "/xml/v1/" };

            foreach (var prefix in unsupportedPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLocalEndpoint(string path)
        {
            // Path is already normalized (starts with /)
            // Check if path matches any local endpoint (defaults without version)
            return LocalEndpoints.Contains(path);
        }

        private async Task MakeLocalRequestAsync(Guid cashBoxId, string accessToken, string method, string path, Dictionary<string, string> headers, string? body)
        {
            // Check for x-operation-id header for idempotency
            string? operationId = null;
            if (headers.TryGetValue("x-operation-id", out var opId))
            {
                operationId = opId;

                // Check if we have a cached result for this operation
                if (OperationCache.TryGetValue(operationId, out var cachedResult))
                {
                    Log.Info(TAG, $"Returning cached result for operation ID: {operationId}");

                    // Return cached result
                    var cachedContentBase64 = Base64UrlHelper.Encode(cachedResult.Content);
                    var cachedContentTypeBase64 = Base64UrlHelper.Encode(cachedResult.ContentType);
                    var cachedHeadersJson = JsonConvert.SerializeObject(cachedResult.Headers);
                    var cachedHeadersBase64 = Base64UrlHelper.Encode(cachedHeadersJson);

                    FinishWithResult(cachedResult.StatusCode, cachedContentBase64, cachedContentTypeBase64, cachedHeadersBase64);
                    return;
                }
            }

            // Check if this is a /v2/echo request with null Message to trigger service restart
            if (string.Equals(path, "/v2/echo", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(body))
            {
                try
                {
                    var echoRequest = JsonConvert.DeserializeObject<EchoRequest>(body);
                    if (echoRequest != null && echoRequest.Message == null)
                    {
                        Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(cashBoxId, accessToken);
                    }
                    else if (LocalMiddlewareServiceInstance == null || !LocalMiddlewareServiceInstance.IsRunning)
                    {
                        Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(cashBoxId, accessToken);
                    }

                    var restartResponse = await LocalMiddlewareServiceInstance.POS.EchoAsync(echoRequest);
                    var restartJson = JsonConvert.SerializeObject(restartResponse);
                    var contentBase64 = Base64UrlHelper.Encode(restartJson);
                    var contentTypeBase64 = Base64UrlHelper.Encode("application/json");
                    var responseHeaders = new Dictionary<string, string>();
                    var headersJson = JsonConvert.SerializeObject(responseHeaders);
                    var headersBase64 = Base64UrlHelper.Encode(headersJson);
                    FinishWithResult("201", contentBase64, contentTypeBase64, headersBase64);
                }
                catch (Exception ex)
                {
                    Log.Warn(TAG, $"Failed to parse echo request body for service restart check: {ex.Message}");
                    // Continue with normal processing if parsing fails
                }
            }

            if (string.Equals(path, "/v2/sign", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(body))
            {
                try
                {
                    var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(body);
                    if (LocalMiddlewareServiceInstance == null || !LocalMiddlewareServiceInstance.IsRunning)
                    {
                        Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(cashBoxId, accessToken);
                    }

                    var restartResponse = await LocalMiddlewareServiceInstance.POS.SignAsync(receiptRequest);
                    var restartJson = JsonConvert.SerializeObject(restartResponse);
                    var contentBase64 = Base64UrlHelper.Encode(restartJson);
                    var contentTypeBase64 = Base64UrlHelper.Encode("application/json");
                    var responseHeaders = new Dictionary<string, string>();
                    var headersJson = JsonConvert.SerializeObject(responseHeaders);
                    var headersBase64 = Base64UrlHelper.Encode(headersJson);
                    FinishWithResult("201", contentBase64, contentTypeBase64, headersBase64);
                }
                catch (Exception ex)
                {
                    Log.Warn(TAG, $"Failed to parse echo request body for service restart check: {ex.Message}");
                    // Continue with normal processing if parsing fails
                }
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            try
            {
                // Ensure path starts with /
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }

                var url = LOCALHOST_BASE_URL.TrimEnd('/') + path;
                Log.Info(TAG, $"Making local HTTP request to {url}");

                // Create HTTP request
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Add headers (skip certain headers that HttpClient handles automatically)
                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Host", "Content-Length", "Connection", "x-operation-id" // x-operation-id is handled separately
                };

                foreach (var header in headers)
                {
                    if (skipHeaders.Contains(header.Key))
                    {
                        continue;
                    }

                    try
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            // Content-Type will be set with content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Failed to add header {header.Key}: {ex.Message}");
                    }
                }

                // Add body if present
                if (!string.IsNullOrEmpty(body))
                {
                    var contentType = headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                // Send request
                var response = await httpClient.SendAsync(request);

                Log.Info(TAG, $"Received local response: {(int)response.StatusCode}");

                await ProcessHttpResponseAsync(response, operationId);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(TAG, $"Local HTTP request failed: {ex.Message}");
                FinishWithError(502, $"Failed to communicate with local middleware: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                Log.Error(TAG, $"Local request timeout: {ex.Message}");
                FinishWithError(504, "Request timeout");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Local request processing failed: {ex}");
                FinishWithError(500, $"Request processing failed: {ex.Message}");
            }
        }

        private async Task RestartMiddlewareLauncherServiceAsync(Guid cashBoxId, string accessToken)
        {
            try
            {
                Log.Info(TAG, "Starting MiddlewareLauncherService restart process");
                await StartMiddlewareLauncherServiceAsync(cashBoxId, accessToken);

                Log.Info(TAG, "MiddlewareLauncherService restart process completed");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Failed to restart MiddlewareLauncherService: {ex.Message}");
            }
        }

        public async Task StartMiddlewareLauncherServiceAsync(Guid cashBoxId, string accessToken)
        {
            LocalMiddlewareServiceInstance = null;
            var isSandbox = true;
            var enableCloseButton = false;
            var logLevel = LogLevel.Debug;
            try
            {
                IntentLauncherService.Stop();
            }
            catch { }

            using var bundle = new Bundle();
            bundle.PutString("cashboxid", cashBoxId.ToString());
            bundle.PutString("accesstoken", accessToken);
            bundle.PutBoolean("sandbox", isSandbox);
            bundle.PutString("loglevel", logLevel.ToString());
            bundle.PutBoolean("enableCloseButton", enableCloseButton);

            IntentLauncherService.Start(cashBoxId.ToString(), accessToken, isSandbox, logLevel, new Dictionary<string, object> { }, enableCloseButton);
            PowerManagerHelper.AskUserToDisableBatteryOptimization(Android.App.Application.Context);

            //using var alarmIntent = new Intent(Android.App.Application.Context, typeof(HttpAlarmReceiver));
            //alarmIntent.PutExtras(bundle);

            //var pending = PendingIntent.GetBroadcast(Android.App.Application.Context, 0, alarmIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            //var alarmManager = (AlarmManager)Android.App.Application.Context.GetSystemService(Context.AlarmService);

            //if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
            //{
            //    alarmManager.SetExact(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 100, pending);
            //}
            //else
            //{
            //    alarmManager.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 100, pending);
            //}

            // Wait for the LocalMiddlewareServiceInstance to be initialized
            await WaitForLocalMiddlewareServiceInitializationAsync();
        }

        private async Task WaitForLocalMiddlewareServiceInitializationAsync()
        {
            const int maxWaitTimeMs = 30000; // 30 seconds timeout
            const int pollIntervalMs = 500;   // Check every 500ms

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Log.Info(TAG, "Waiting for LocalMiddlewareServiceInstance to be initialized...");

            while (stopwatch.ElapsedMilliseconds < maxWaitTimeMs)
            {
                if (LocalMiddlewareServiceInstance != null && LocalMiddlewareServiceInstance.IsRunning)
                {
                    Log.Info(TAG, $"LocalMiddlewareServiceInstance initialized after {stopwatch.ElapsedMilliseconds}ms");
                    return;
                }

                await Task.Delay(pollIntervalMs);
            }

            Log.Warn(TAG, $"Timeout waiting for LocalMiddlewareServiceInstance initialization after {stopwatch.ElapsedMilliseconds}ms");
            throw new TimeoutException("LocalMiddlewareServiceInstance failed to initialize within the expected time");
        }

        private async Task MakeCloudRequestAsync(string method, string path, Dictionary<string, string> headers, string? body)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            try
            {
                // Determine if sandbox or production based on headers or use default
                var isSandbox = headers.TryGetValue("x-sandbox", out var sandbox) &&
                                (sandbox == "true" || sandbox == "1");

                var baseUrl = isSandbox ? Urls.POSSYSTEM_API_SANDBOX : Urls.POSSYSTEM_API_PRODUCTION;

                // Ensure path starts with /
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }

                var url = baseUrl.TrimEnd('/') + path;
                Log.Info(TAG, $"Making cloud HTTP request to {url}");

                // Create HTTP request
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Add headers (skip certain headers that HttpClient handles automatically)
                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Host", "Content-Length", "Connection", "x-sandbox" // x-sandbox is only for routing
                };

                foreach (var header in headers)
                {
                    if (skipHeaders.Contains(header.Key))
                    {
                        continue;
                    }

                    try
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            // Content-Type will be set with content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Failed to add header {header.Key}: {ex.Message}");
                    }
                }

                // Add body if present
                if (!string.IsNullOrEmpty(body))
                {
                    var contentType = headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                // Send request
                var response = await httpClient.SendAsync(request);

                Log.Info(TAG, $"Received cloud response: {(int)response.StatusCode}");

                await ProcessHttpResponseAsync(response, null); // No caching for cloud requests
            }
            catch (HttpRequestException ex)
            {
                Log.Error(TAG, $"Cloud HTTP request failed: {ex.Message}");
                FinishWithError(502, $"Failed to communicate with cloud PosSystemAPI: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                Log.Error(TAG, $"Cloud request timeout: {ex.Message}");
                FinishWithError(504, "Request timeout");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Cloud request processing failed: {ex}");
                FinishWithError(500, $"Request processing failed: {ex.Message}");
            }
        }

        private async Task ProcessHttpResponseAsync(HttpResponseMessage response, string? operationId)
        {
            // Read response
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            // Build response headers dictionary
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            // Cache the result if we have an operation ID (local requests only)
            if (!string.IsNullOrEmpty(operationId))
            {
                var cachedResult = new CachedOperationResult(
                    ((int)response.StatusCode).ToString(),
                    responseContent,
                    responseContentType,
                    responseHeaders
                );

                OperationCache.TryAdd(operationId, cachedResult);
                Log.Info(TAG, $"Cached result for operation ID: {operationId}");
            }

            // Encode response
            var statusCode = ((int)response.StatusCode).ToString();
            var contentBase64Url = Base64UrlHelper.Encode(responseContent);
            var contentTypeBase64Url = Base64UrlHelper.Encode(responseContentType);
            var responseHeadersJson = JsonConvert.SerializeObject(responseHeaders);
            var responseHeadersBase64Url = Base64UrlHelper.Encode(responseHeadersJson);

            // Return result
            FinishWithResult(statusCode, contentBase64Url, contentTypeBase64Url, responseHeadersBase64Url);
        }

        private void FinishWithResult(string statusCode, string contentBase64Url, string contentTypeBase64Url, string? headerBase64Url = null)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var resultIntent = new Intent();
                    resultIntent.PutExtra(EXTRA_STATUS_CODE, statusCode);
                    resultIntent.PutExtra(EXTRA_CONTENT_BASE64URL, contentBase64Url);
                    resultIntent.PutExtra(EXTRA_CONTENT_TYPE_BASE64URL, contentTypeBase64Url);

                    if (!string.IsNullOrEmpty(headerBase64Url))
                    {
                        resultIntent.PutExtra(EXTRA_RESPONSE_HEADER_JSON_BASE64URL, headerBase64Url);
                    }

                    SetResult(Result.Ok, resultIntent);
                    Log.Info(TAG, $"Finishing with success: {statusCode}");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to set result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }

        private void FinishWithError(int statusCode, string errorMessage)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var errorResponse = new
                    {
                        type = "https://tools.ietf.org/html/rfc9110",
                        title = "Error",
                        status = statusCode,
                        detail = errorMessage
                    };

                    var errorJson = JsonConvert.SerializeObject(errorResponse);
                    var contentBase64Url = Base64UrlHelper.Encode(errorJson);
                    var contentTypeBase64Url = Base64UrlHelper.Encode("application/json");

                    var resultIntent = new Intent();
                    resultIntent.PutExtra(EXTRA_STATUS_CODE, statusCode.ToString());
                    resultIntent.PutExtra(EXTRA_CONTENT_BASE64URL, contentBase64Url);
                    resultIntent.PutExtra(EXTRA_CONTENT_TYPE_BASE64URL, contentTypeBase64Url);

                    SetResult(Result.Ok, resultIntent);
                    Log.Info(TAG, $"Finishing with error: {statusCode} - {errorMessage}");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to set error result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }
    }

    /// <summary>
    /// Represents a cached operation result for idempotency
    /// </summary>
    internal class CachedOperationResult
    {
        public string StatusCode { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public DateTime CachedAt { get; set; }

        public CachedOperationResult(string statusCode, string content, string contentType, Dictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Content = content;
            ContentType = contentType;
            Headers = headers;
            CachedAt = DateTime.UtcNow;
        }
    }
}
