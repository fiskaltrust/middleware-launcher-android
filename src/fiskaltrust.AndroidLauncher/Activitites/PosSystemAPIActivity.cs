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
                FinishWithResponse(PosSystemApiResponse.Error(500, "Intent is null"));
                return;
            }

            try
            {
                var request = PosSystemApiRequestExtensions.FromIntent(intent);

                Log.Info(TAG, $"Processing request: {request.Method} {request.Path}");

                var response = await SendRequestToServiceAsync(request).ConfigureAwait(false);
                FinishWithResponse(response);
            }
            catch (ArgumentException ex)
            {
                Log.Error(TAG, $"Invalid request: {ex.Message}");
                FinishWithResponse(PosSystemApiResponse.Error(400, ex.Message));
            }
            catch (TimeoutException ex)
            {
                Log.Error(TAG, $"Request processing timeout: {ex}");
                FinishWithResponse(PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Request processing failed: {ex}");
                FinishWithResponse(PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}"));
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
