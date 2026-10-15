using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.AndroidService;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.Api.PosSystem.Core.Models;
using Microsoft.Maui.Controls.Embedding;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    [Activity(
        Label = "PosSystemAPI",
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPI",
        Enabled = true,
        Exported = true)]
    // Lets browser-based callers start this activity via an `intent://` link
    // (scheme=fiskaltrust, host=possystemapi). The request itself is still read
    // from the intent extras; the data URI is not used.
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "fiskaltrust",
        DataHost = "possystemapi")]
    public class PosSystemAPIActivity : Activity
    {
        private const string TAG = "PosSystemAPI";

        private static readonly TimeSpan BindTimeout = TimeSpan.FromSeconds(30);

        private ServiceBinderConnection? _connection;
        private bool _bound;
        private PosSystemApiView? _view;

        public PosSystemAPIActivity()
        {
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            (Microsoft.Maui.Controls.Application.Current as IApplication)?.ThemeChanged();
            var services = IPlatformApplication.Current!.Services;
            var context = new MauiContext(services, this);
            _view = new PosSystemApiView();
            SetContentView(_view.ToPlatformEmbedded(context));

            PopulateEndpointPreview(Intent);

            Log.Info(TAG, "PosSystemAPI Activity started");

            // Process the intent asynchronously
            Task.Run(ProcessIntentAsync);
        }

        protected override void OnDestroy()
        {
            UnbindFromService();

            base.OnDestroy();
        }

        private async Task ProcessIntentAsync()
        {
            var intent = Intent;
            if (intent == null)
            {
                Log.Error(TAG, "Intent is null");
                Respond(PosSystemApiResponse.Error(500, "Intent is null"));
                return;
            }

            var callbackUrl = intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_CALLBACK_URL);
            if (!string.IsNullOrEmpty(callbackUrl) && !IsAllowedCallbackUrl(callbackUrl))
            {
                // Rejected before the request is executed so no side effects occur for a response that can't be delivered.
                Log.Error(TAG, "Rejected callback URL: only https, or http for localhost, is allowed");
                FinishWithResponse(PosSystemApiResponse.Error(400, "CallbackUrl must use https, or http for localhost"));
                return;
            }

            try
            {
                var request = PosSystemApiRequestExtensions.FromIntent(intent);

                Log.Info(TAG, $"Processing request: {request.Method} {request.Path}");

                var response = await SendRequestToServiceAsync(request).ConfigureAwait(false);
                Respond(response);
            }
            catch (ArgumentException ex)
            {
                Log.Error(TAG, $"Invalid request: {ex.Message}");
                Respond(PosSystemApiResponse.Error(400, ex.Message));
            }
            catch (TimeoutException ex)
            {
                Log.Error(TAG, $"Request processing timeout: {ex}");
                Respond(PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Request processing failed: {ex}");
                Respond(PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Binds to <see cref="PosSystemAPIService"/> with the local-bind action and
        /// invokes the request handler directly through the local binder. The request
        /// is parsed before binding, so malformed requests fail without starting the
        /// service.
        /// </summary>
        private async Task<PosSystemApiResponse> SendRequestToServiceAsync(PosSystemApiRequest request)
        {
            _connection = new ServiceBinderConnection();

            var serviceIntent = new Intent(this, typeof(PosSystemAPIService));
            serviceIntent.SetAction(PosSystemAPIService.ActionLocalBind);
            _bound = BindService(serviceIntent, _connection, Bind.AutoCreate);
            if (!_bound)
                throw new InvalidOperationException("Failed to bind to PosSystemAPIService");

            var binder = await _connection.BinderTask.WaitAsync(BindTimeout).ConfigureAwait(false);

            if (binder is not PosSystemAPIService.LocalBinder localBinder)
                throw new InvalidOperationException("Unexpected binder type returned by PosSystemAPIService");

            return await localBinder.HandleRequestAsync(request, SetProgressText).ConfigureAwait(false);
        }


        /// <summary>
        /// Allows https URLs, and http only for loopback hosts (localhost, 127.0.0.1, ::1).
        /// Uses the parsed host, so tricks like http://localhost@evil.example are not treated as loopback.
        /// </summary>
        private static bool IsAllowedCallbackUrl(string callbackUrl)
        {
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttps
                || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        }

        private void Respond(PosSystemApiResponse response) {
            var callbackUrl = Intent?.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_CALLBACK_URL);

            if (!string.IsNullOrEmpty(callbackUrl))
            {
                FinishWithCallbackUrl(callbackUrl, response);
            } else {
                FinishWithResponse(response);
            }
        }
        /// <summary>
        /// Finishes the activity with a structured response using the DTO.
        /// Used for errors raised locally, before or instead of a service reply.
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
                    var contentForLog = response.Content is ResponseBody.Text t ? t.Value : "(binary)";
                    Log.Info(TAG, $"Finishing with response: {response.StatusCode} - {(response.IsSuccess ? "Success" : "Error")} - {contentForLog}");
                }
                catch (InvalidOperationException ex)
                {
                    Log.Error(TAG, $"Failed to set response result: {ex}");
                }
                catch (ArgumentException ex)
                {
                    Log.Error(TAG, $"Failed to set response result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }

        /// <summary>
        /// Redirects to the caller's callback URL with response data as query parameters.
        /// Works like an OAuth redirect: the browser navigates back to the webapp with the response.
        /// If the redirect cannot be started (e.g. the payload is too large for the intent),
        /// a small 500 response is sent to the callback URL instead so the caller is not left waiting.
        /// </summary>
        private void FinishWithCallbackUrl(string callbackUrl, PosSystemApiResponse response)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    if (!TryRedirectToCallbackUrl(callbackUrl, response))
                    {
                        TryRedirectToCallbackUrl(
                            callbackUrl,
                            PosSystemApiResponse.Error(500, "Failed to deliver the response to the callback URL"));
                    }
                }
                finally
                {
                    Finish();
                }
            });
        }

        private bool TryRedirectToCallbackUrl(string callbackUrl, PosSystemApiResponse response)
        {
            try
            {
                var intentData = response.ToIntentData();

                // Separate URL from hash fragment so query params are inserted before the hash.
                // e.g. https://app.com/?foo=1#/route -> insert &StatusCode=... before #/route
                var hashIndex = callbackUrl.IndexOf('#', StringComparison.Ordinal);
                var urlPart = hashIndex >= 0 ? callbackUrl[..hashIndex] : callbackUrl;
                var hashPart = hashIndex >= 0 ? callbackUrl[hashIndex..] : "";

                var separator = urlPart.Contains('?', StringComparison.Ordinal) ? "&" : "?";
                var redirectUrl = $"{urlPart}{separator}" +
                    $"StatusCode={Uri.EscapeDataString(intentData.StatusCode)}" +
                    $"&ContentBase64Url={Uri.EscapeDataString(intentData.ContentBase64Url)}" +
                    $"&ContentTypeBase64Url={Uri.EscapeDataString(intentData.ContentTypeBase64Url)}";

                if (!string.IsNullOrEmpty(intentData.HeadersBase64Url))
                {
                    redirectUrl += $"&HeaderJsonObjectBase64Url={Uri.EscapeDataString(intentData.HeadersBase64Url)}";
                }

                redirectUrl += hashPart;

                Log.Info(TAG, $"Redirecting to callback URL with status {response.StatusCode}");

                using var browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(redirectUrl));
                browserIntent.AddFlags(ActivityFlags.NewTask);
                StartActivity(browserIntent);
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Java.Lang.Exception)
            {
                Log.Error(TAG, $"Failed to redirect to callback URL: {ex}");
                return false;
            }
        }

        private void UnbindFromService()
        {
            if (_bound && _connection != null)
            {
                try
                {
                    UnbindService(_connection);
                }
                catch (Exception ex)
                {
                    Log.Warn(TAG, $"UnbindService failed: {ex.Message}");
                }
            }
            _bound = false;
            _connection = null;
        }

        private void PopulateEndpointPreview(Intent? intent)
        {
            var method = intent?.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_METHOD);
            var path = intent?.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_PATH);

            _view?.SetEndpoint($"{method?.ToUpperInvariant()} {path}");
        }

        private void SetProgressText(string text)
        {
            RunOnUiThread(() => _view?.SetStage(text));
        }

        private sealed class ServiceBinderConnection : Java.Lang.Object, IServiceConnection
        {
            private readonly TaskCompletionSource<IBinder> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<IBinder> BinderTask => _tcs.Task;

            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                if (service != null)
                    _tcs.TrySetResult(service);
                else
                    _tcs.TrySetException(new InvalidOperationException("Service connected with a null binder"));
            }

            public void OnServiceDisconnected(ComponentName? name)
            {
            }

            public void OnNullBinding(ComponentName? name)
            {
                _tcs.TrySetException(new InvalidOperationException("PosSystemAPIService returned a null binding"));
            }
        }
    }
}
