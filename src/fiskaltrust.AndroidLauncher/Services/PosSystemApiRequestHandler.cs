using Android.Util;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Notifications;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.Api.PosSystem.Core;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.ifPOS.v2;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace fiskaltrust.AndroidLauncher.Services
{
    internal class PosSystemApiRequestHandler
    {
        private const string TAG = "PosSystemAPI";
        private readonly PosSystemAPIProvider _posSystemAPIProvider;

        private static readonly HashSet<string> LocalEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/v2/sign",
            "/v2/echo",
            "/v2/journal",
        };

        public PosSystemApiRequestHandler(IConfigurationProvider configurationProvider, ILauncherStateNotifier stateNotifier)
        {
            _posSystemAPIProvider = new PosSystemAPIProvider(configurationProvider, stateNotifier);
        }

        public async Task<PosSystemApiResponse> HandleAsync(PosSystemApiRequest request, Action<string>? progressReporter = null)
        {
            try
            {
                var isLocalEndpoint = request.IsLocalEndpoint(LocalEndpoints);
                if (isLocalEndpoint)
                {
                    Log.Info(TAG, $"Routing to local middleware: {request.Path}");
                    return await MakeLocalRequestAsync(request, progressReporter).ConfigureAwait(false);
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

        private async Task<PosSystemApiResponse> MakeLocalRequestAsync(PosSystemApiRequest request, Action<string>? progressReporter)
        {
            if (string.Equals(request.Path, "/v2/echo", StringComparison.OrdinalIgnoreCase))
            {
                var echoRequest = JsonSerializer.Deserialize<EchoRequest>(request.Body ?? "");
                if (echoRequest?.Message == null)
                {
                    Log.Info(TAG, "Detected /v2/echo request with null Message - triggering service restart");
                    await _posSystemAPIProvider.Restart(request.CashBoxId!.Value, request.AccessToken, progressReporter);
                }
            }

            try
            {
                var core = await _posSystemAPIProvider.Get(request.CashBoxId!.Value, request.AccessToken, progressReporter).ConfigureAwait(false);
                progressReporter?.Invoke(ActivityStages.STAGE_PROCESSING_REQUEST);
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
            using var httpClient = new HttpClient();

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
    }
}
