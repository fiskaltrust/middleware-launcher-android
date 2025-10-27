using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling;
using fiskaltrust.ifPOS.v2;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

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

        public static OperationStateMachine? OperationStateMachine { get; set; }

        private const string TAG = "PosSystemAPI";

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

                // Parse the intent into a DTO
                PosSystemApiRequest request;
                try
                {
                    request = PosSystemApiRequestExtensions.FromIntent(intent);
                    Log.Info(TAG, $"Processing request: {request.Method} {request.Path}");
                }
                catch (ArgumentException ex)
                {
                    Log.Error(TAG, $"Invalid request: {ex.Message}");
                    FinishWithError(400, ex.Message);
                    return;
                }

                // Validate endpoint version - only support defaults (no version) and /v2
                if (!request.IsValidVersion())
                {
                    Log.Error(TAG, $"Unsupported endpoint version: {request.NormalizedPath}");
                    FinishWithError(400, $"Unsupported endpoint version. Only default endpoints (e.g., /sign, /echo) and /v2/* endpoints are supported. Please do not use /v0/* or /v1/* versions.");
                    return;
                }

                // Determine if this is a local or cloud endpoint
                var isLocalEndpoint = request.IsLocalEndpoint(LocalEndpoints);

                if (isLocalEndpoint)
                {
                    Log.Info(TAG, $"Routing to local middleware: {request.NormalizedPath}");
                    await MakeLocalRequestAsync(request);
                }
                else
                {
                    Log.Info(TAG, $"Routing to cloud PosSystemAPI: {request.NormalizedPath}");
                    await MakeCloudRequestAsync(request);
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Unexpected error: {ex}");
                FinishWithError(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task MakeLocalRequestAsync(PosSystemApiRequest request)
        {
            // Check if this is a /v2/echo request with null Message to trigger service restart
            if (string.Equals(request.NormalizedPath, "/v2/echo", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var echoRequest = JsonSerializer.Deserialize<EchoRequest>(request.Body);
                    if (echoRequest != null && echoRequest.Message == null)
                    {
                        Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(request.CashBoxId, request.AccessToken);
                    }
                    else if (LocalMiddlewareServiceInstance == null || !LocalMiddlewareServiceInstance.IsRunning)
                    {
                        Log.Info(TAG, "Local middleware not running - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(request.CashBoxId, request.AccessToken);
                    }
                    var echoResponse = await OperationStateMachine.PerformEchoAsync(request);
                    if (echoResponse.IsOk)
                    {
                        var responseJson = JsonSerializer.Serialize(echoResponse.OkValue.Value, new JsonSerializerOptions
                        {
                            // Approach B: the broad “unsafe relaxed” encoder that reduces escaping significantly:
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = true
                        });
                        var response = PosSystemApiResponse.Success(responseJson, "application/json", "200");
                        FinishWithResponse(response);
                    }
                    else
                    {
                        var problemDetails = echoResponse.ErrValue;
                        var errorResponse = PosSystemApiResponse.Error(problemDetails.Status ?? 500, problemDetails.Detail ?? "Unknown error", problemDetails.Title ?? "Error");
                        FinishWithResponse(errorResponse);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to process echo request: {ex.Message}");
                    var errorResponse = PosSystemApiResponse.Error(500, $"Failed to process echo request: {ex.Message}");
                    FinishWithResponse(errorResponse);
                    return;
                }
            }

            if (string.Equals(request.NormalizedPath, "/v2/sign", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(request.Body);
                    if (LocalMiddlewareServiceInstance == null || !LocalMiddlewareServiceInstance.IsRunning)
                    {
                        Log.Info(TAG, "Local middleware not running - triggering service restart");
                        await RestartMiddlewareLauncherServiceAsync(request.CashBoxId, request.AccessToken);
                    }
                    var signResponse = await OperationStateMachine.PerformSignAsync(request);
                    if (signResponse.IsOk)
                    {
                        var responseJson = JsonSerializer.Serialize(signResponse.OkValue.Value, new JsonSerializerOptions
                        {
                            // Approach B: the broad “unsafe relaxed” encoder that reduces escaping significantly:
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = true
                        });
                        var response = PosSystemApiResponse.Success(responseJson, "application/json", "200");
                        FinishWithResponse(response);
                    }
                    else
                    {
                        var problemDetails = signResponse.ErrValue;
                        var errorResponse = PosSystemApiResponse.Error(problemDetails.Status ?? 500, problemDetails.Detail ?? "Unknown error", problemDetails.Title ?? "Error");
                        FinishWithResponse(errorResponse);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to process sign request: {ex.Message}");
                    var errorResponse = PosSystemApiResponse.Error(500, $"Failed to process sign request: {ex.Message}");
                    FinishWithResponse(errorResponse);
                    return;
                }
            }
            
            var notSupportedResponse = PosSystemApiResponse.Error(400, $"The selected path '{request.NormalizedPath}' and method '{request.Method}' is not supported.");
            FinishWithResponse(notSupportedResponse);
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
                MiddlewareLauncherService.Stop();
            }
            catch { }

            using var bundle = new Bundle();
            bundle.PutString("cashboxid", cashBoxId.ToString());
            bundle.PutString("accesstoken", accessToken);
            bundle.PutBoolean("sandbox", isSandbox);
            bundle.PutString("loglevel", logLevel.ToString());
            bundle.PutBoolean("enableCloseButton", enableCloseButton);

            PowerManagerHelper.AskUserToDisableBatteryOptimization(Android.App.Application.Context);
            MiddlewareLauncherService.Start(cashBoxId.ToString(), accessToken, isSandbox, logLevel, new Dictionary<string, object> { }, enableCloseButton);
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

        private async Task MakeCloudRequestAsync(PosSystemApiRequest request)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            try
            {
                var baseUrl = request.IsSandbox ? Urls.POSSYSTEM_API_SANDBOX : Urls.POSSYSTEM_API_PRODUCTION;

                var url = baseUrl.TrimEnd('/') + request.NormalizedPath;
                Log.Info(TAG, $"Making cloud HTTP request to {url}");

                // Create HTTP request
                var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), url);

                // Add headers (skip certain headers that HttpClient handles automatically)
                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Host", "Content-Length", "Connection", "x-sandbox" // x-sandbox is only for routing
                };

                foreach (var header in request.Headers)
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
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Failed to add header {header.Key}: {ex.Message}");
                    }
                }

                // Add body if present
                if (!string.IsNullOrEmpty(request.Body))
                {
                    var contentType = request.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                }

                // Send request
                var httpResponse = await httpClient.SendAsync(httpRequest);

                Log.Info(TAG, $"Received cloud response: {(int)httpResponse.StatusCode}");

                // Convert HTTP response to our DTO
                var response = await PosSystemApiResponse.FromHttpResponseAsync(httpResponse);
                FinishWithResponse(response);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(TAG, $"Cloud HTTP request failed: {ex.Message}");
                var errorResponse = PosSystemApiResponse.Error(502, $"Failed to communicate with cloud PosSystemAPI: {ex.Message}");
                FinishWithResponse(errorResponse);
            }
            catch (TaskCanceledException ex)
            {
                Log.Error(TAG, $"Cloud request timeout: {ex.Message}");
                var errorResponse = PosSystemApiResponse.Error(504, "Request timeout");
                FinishWithResponse(errorResponse);
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Cloud request processing failed: {ex}");
                var errorResponse = PosSystemApiResponse.Error(500, $"Request processing failed: {ex.Message}");
                FinishWithResponse(errorResponse);
            }
        }

        /// <summary>
        /// Finishes the activity with a structured response using the DTO
        /// </summary>
        /// <param name="response">The PosSystemApiResponse to return</param>
        private void FinishWithResponse(PosSystemApiResponse response)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var intentData = response.ToIntentData();
                    var resultIntent = new Intent();
                    
                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_STATUS_CODE, intentData.StatusCode);
                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_CONTENT_BASE64URL, intentData.ContentBase64Url);
                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_CONTENT_TYPE_BASE64URL, intentData.ContentTypeBase64Url);

                    if (!string.IsNullOrEmpty(intentData.HeadersBase64Url))
                    {
                        resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_RESPONSE_HEADER_JSON_BASE64URL, intentData.HeadersBase64Url);
                    }

                    SetResult(Result.Ok, resultIntent);
                    Log.Info(TAG, $"Finishing with response: {response.StatusCode} - {(response.IsSuccess ? "Success" : "Error")}");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to set response result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }

        private void FinishWithError(int statusCode, string errorMessage)
        {
            var errorResponse = PosSystemApiResponse.Error(statusCode, errorMessage);
            FinishWithResponse(errorResponse);
        }
    }
}
