using Android.Util;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.ifPOS.v2;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class PosSystemApiRequestHandler
    {
        private const string TAG = "PosSystemAPI";
        private readonly Action<string>? _progressReporter;

        private static readonly HashSet<string> LocalEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/sign",
            "/v2/sign",
            "/echo",
            "/v2/echo",
            "/journal",
            "/v2/journal",
        };

        public PosSystemApiRequestHandler(Action<string>? progressReporter = null)
        {
            _progressReporter = progressReporter;
        }

        public async Task<PosSystemApiResponse> HandleAsync(PosSystemApiRequest request)
        {
            try
            {
                if (!request.IsValidVersion())
                {
                    Log.Error(TAG, $"Unsupported endpoint version: {request.Path}");
                    return PosSystemApiResponse.Error(
                        400,
                        $"Unsupported endpoint version. Only default endpoints (e.g., /sign, /echo) and /v2/* endpoints are supported. Please do not use /v0/* or /v1/* versions.");
                }

                var isLocalEndpoint = request.IsLocalEndpoint(LocalEndpoints);
                if (isLocalEndpoint)
                {
                    Log.Info(TAG, $"Routing to local middleware: {request.Path}");
                    return await MakeLocalRequestAsync(request).ConfigureAwait(false);
                }
                else
                {
                    Log.Info(TAG, $"Routing to cloud PosSystemAPI: {request.Path}");
                    return await MakeCloudRequestAsync(request).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Unexpected error: {ex}");
                return PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task<PosSystemApiResponse> MakeLocalRequestAsync(PosSystemApiRequest request)
        {
            _progressReporter?.Invoke(ActivityStages.STAGE_STARTING_MIDDLEWARE);
            await EnsureSystemReadyAsync((Guid)request.CashBoxId!, request.AccessToken).ConfigureAwait(false);
            if(request.Path == "/v2/echo") {throw new InvalidOperationException("Simulated error for testing purposes.");}
            var supportedPaths = new[] { "/v2/echo", "/v2/sign", "/v2/journal" };
            if (!supportedPaths.Contains(request.Path, StringComparer.OrdinalIgnoreCase))
            {
                return PosSystemApiResponse.Error(
                    400,
                    $"The selected path '{request.Path}' and method '{request.Method}' is not supported.");
            }

            if (string.Equals(request.Path, "/v2/echo", StringComparison.OrdinalIgnoreCase))
            {
                var echoRequest = JsonSerializer.Deserialize<EchoRequest>(request.Body ?? "");
                if (echoRequest?.Message == null)
                {
                    Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                    _progressReporter?.Invoke(ActivityStages.STAGE_STARTING_MIDDLEWARE);
                    await RestartMiddlewareLauncherServiceAsync((Guid)request.CashBoxId, request.AccessToken).ConfigureAwait(false);
                }
            }

            try
            {
                _progressReporter?.Invoke(ActivityStages.STAGE_STARTING_CORE);
                var core = LauncherRuntimeState.PosSystemApiCore
                    ?? throw new InvalidOperationException("PosSystemApiCore is not initialized.");
                _progressReporter?.Invoke(ActivityStages.STAGE_PROCESSING_REQUEST);
                return await core.HandleAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var endpoint = request.Path.Split('/').Last();
                Log.Error(TAG, $"Failed to process {endpoint} request: {ex.Message}");
                return PosSystemApiResponse.Error(500, $"Failed to process {endpoint} request: {ex.Message}");
            }
        }

        private async Task<PosSystemApiResponse> MakeCloudRequestAsync(PosSystemApiRequest request)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
            };

            try
            {
                var baseUrl = Urls.POSSYSTEM_API_SANDBOX;
                var path = request.Path;
                if (!path.StartsWith("/v2", StringComparison.OrdinalIgnoreCase))
                {
                    path = "/v2" + path;
                }
                var url = baseUrl.TrimEnd('/') + path;
                Log.Info(TAG, $"Making cloud HTTP request to {url}");

                var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), url);

                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Host", "Content-Length", "Connection"
                };

                foreach (var header in request.Headers)
                {
                    if (skipHeaders.Contains(header.Key))
                        continue;

                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Failed to add header {header.Key}: {ex.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(request.Body))
                {
                    var contentType = request.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                }

                var httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
                Log.Info(TAG, $"Received cloud response: {(int)httpResponse.StatusCode}");

                return await PosSystemApiResponseExtensions.FromHttpResponseAsync(httpResponse).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(TAG, $"Cloud HTTP request failed: {ex.Message}");
                return PosSystemApiResponse.Error(502, $"Failed to communicate with cloud PosSystemAPI: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                Log.Error(TAG, $"Cloud request timeout: {ex.Message}");
                return PosSystemApiResponse.Error(504, "Request timeout");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Cloud request processing failed: {ex}");
                return PosSystemApiResponse.Error(500, $"Request processing failed: {ex.Message}");
            }
        }

        private async Task EnsureSystemReadyAsync(Guid cashBoxId, string accessToken, CancellationToken cancellationToken = default)
        {
            const int maxWaitTimeMs = 10_000;
            const int pollIntervalMs = 100;

            if (LauncherRuntimeState.LocalMiddlewareServiceInstance == null
                || !LauncherRuntimeState.LocalMiddlewareServiceInstance.IsRunning)
            {
                Log.Info(TAG, "Local middleware not running - triggering service restart");
                _progressReporter?.Invoke(ActivityStages.STAGE_STARTING_MIDDLEWARE);
                await RestartMiddlewareLauncherServiceAsync(cashBoxId, accessToken).ConfigureAwait(false);
            }

            _progressReporter?.Invoke(ActivityStages.STAGE_STARTING_CORE);
            var waitedMs = 0;
            while (LauncherRuntimeState.PosSystemApiCore == null && waitedMs < maxWaitTimeMs)
            {
                await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
                waitedMs += pollIntervalMs;
            }

            if (LauncherRuntimeState.PosSystemApiCore == null)
            {
                throw new TimeoutException("POS system API core did not become ready in time.");
            }
        }

        private async Task RestartMiddlewareLauncherServiceAsync(Guid cashBoxId, string accessToken)
        {
            try
            {
                Log.Info(TAG, "Starting MiddlewareLauncherService restart process");
                await StartMiddlewareLauncherServiceAsync(cashBoxId, accessToken).ConfigureAwait(false);
                Log.Info(TAG, "MiddlewareLauncherService restart process completed");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Failed to restart MiddlewareLauncherService: {ex.Message}");
            }
        }

        private async Task StartMiddlewareLauncherServiceAsync(Guid cashBoxId, string accessToken)
        {
            LauncherRuntimeState.LocalMiddlewareServiceInstance = null;
            var isSandbox = true;
            var enableCloseButton = false;
            var logLevel = LogLevel.Debug;
            try
            {
                MiddlewareLauncherService.Stop();
            }
            catch { }

            PowerManagerHelper.AskUserToDisableBatteryOptimization(Android.App.Application.Context);
            MiddlewareLauncherService.Start(cashBoxId.ToString(), accessToken, isSandbox, logLevel, new Dictionary<string, object>(), enableCloseButton);
            await WaitForLocalMiddlewareServiceInitializationAsync().ConfigureAwait(false);
        }

        private async Task WaitForLocalMiddlewareServiceInitializationAsync()
        {
            const int maxWaitTimeMs = 30_000;
            const int pollIntervalMs = 500;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Log.Info(TAG, "Waiting for LocalMiddlewareServiceInstance to be initialized...");

            while (stopwatch.ElapsedMilliseconds < maxWaitTimeMs)
            {
                var instance = LauncherRuntimeState.LocalMiddlewareServiceInstance;
                if (instance != null && instance.IsRunning)
                {
                    Log.Info(TAG, $"LocalMiddlewareServiceInstance initialized after {stopwatch.ElapsedMilliseconds}ms");
                    return;
                }
                await Task.Delay(pollIntervalMs).ConfigureAwait(false);
            }

            Log.Warn(TAG, $"Timeout waiting for LocalMiddlewareServiceInstance initialization after {stopwatch.ElapsedMilliseconds}ms");
            throw new TimeoutException("LocalMiddlewareServiceInstance failed to initialize within the expected time");
        }
    }
}
