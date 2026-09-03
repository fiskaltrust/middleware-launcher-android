using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Notifications;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.AndroidLauncher.Services.Configuration;
using fiskaltrust.Api.PosSystem.Core.Models;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.AndroidService
{
    [Service(
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPIService",
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeSpecialUse)]
    public class PosSystemAPIService : Service
    {
        public const string ActionLocalBind = "eu.fiskaltrust.androidlauncher.action.LOCAL_BIND";

        private const string TAG = "PosSystemAPIService";

        private HandlerThread? _handlerThread;
        private Messenger? _messenger;
        private PosSystemApiRequestHandler? _requestHandler;

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Info(TAG, "OnCreate");

            _handlerThread = new HandlerThread("PosSystemAPIServiceThread");
            _handlerThread.Start();

            _requestHandler = new PosSystemApiRequestHandler(new ConfigurationProvider(
                new HelipadConfigurationProvider(),
                new LocalConfigurationProvider()),
                LauncherNotificationHandler.Instance);

            _messenger = new Messenger(new IncomingHandler(_handlerThread.Looper!, _requestHandler));

            LauncherNotificationHandler.Instance.EnsureNotificationChannel();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Log.Info(TAG, "OnStartCommand");

            var notification = LauncherNotificationHandler.Instance.BuildCurrentNotification();
            if (Build.VERSION.SdkInt > BuildVersionCodes.Tiramisu)
            {
                // Android 14 requires us to specify the service type
                StartForeground(LauncherNotificationHandler.NotificationId, notification, ForegroundService.TypeSpecialUse);
            }
            else
            {
                StartForeground(LauncherNotificationHandler.NotificationId, notification);
            }

            return StartCommandResult.Sticky;
        }

        /// <summary>
        /// Called by the system when the foreground service exceeds its type-specific
        /// runtime budget (e.g. the 6h/24h cap Android 15+ applies to some types). We
        /// must stop within a few seconds or the system kills the app with an ANR.
        /// The next request re-binds and re-promotes the service, restarting the
        /// middleware.
        /// </summary>
        public override void OnTimeout(int startId, [GeneratedEnum] ForegroundService fgsType)
        {
            Log.Warn(TAG, $"Foreground service runtime budget exhausted (type={fgsType}); stopping service.");
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }

        public override IBinder? OnBind(Intent? intent)
        {
            Log.Info(TAG, "OnBind");
            PromoteToForegroundService();

            if (intent?.Action == ActionLocalBind)
                return _requestHandler is { } handler ? new LocalBinder(handler) : null;

            return _messenger?.Binder;
        }

        /// <summary>
        /// Starts this service as a foreground service so it survives unbinding and
        /// shows the middleware state notification, regardless of which client bound
        /// to it. A bound-only service would be destroyed on the last unbind, even
        /// with foreground status.
        /// </summary>
        private void PromoteToForegroundService()
        {
            try
            {
                this.StartForegroundServiceCompat<PosSystemAPIService>();
            }
            catch (Exception ex)
            {
                // Android 12+ forbids starting a foreground service while the app is in
                // the background (e.g. bound by a non-visible client). Fall back to
                // bound-only operation: requests still work, but the service won't
                // outlive the binding.
                Log.Warn(TAG, $"Could not promote to foreground service: {ex.Message}");
            }
        }

        public override void OnDestroy()
        {
            Log.Info(TAG, "OnDestroy");
            try
            {
                _handlerThread?.QuitSafely();
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"HandlerThread quit failed: {ex.Message}");
            }
            _handlerThread = null;
            _messenger = null;
            _requestHandler = null;

            try
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"StopForeground failed: {ex.Message}");
            }

            base.OnDestroy();
        }


        /// <summary>
        /// Binder handed out to in-process clients (bound with <see cref="ActionLocalBind"/>).
        /// It captures the request handler at bind time, so callers can never observe a
        /// torn-down service (no null race with <see cref="OnDestroy"/>). Callers parse
        /// and validate their raw input into a <see cref="PosSystemApiRequest"/> via the
        /// shared <see cref="PosSystemApiRequestExtensions"/> helpers before calling, so
        /// requests reach the handler identically to the Messenger path.
        /// </summary>
        public sealed class LocalBinder : Binder
        {
            private readonly PosSystemApiRequestHandler _requestHandler;

            internal LocalBinder(PosSystemApiRequestHandler requestHandler)
            {
                _requestHandler = requestHandler;
            }

            public Task<PosSystemApiResponse> HandleRequestAsync(PosSystemApiRequest request, Action<string> progressReporter)
            {
                return _requestHandler.HandleAsync(request, progressReporter);
            }
        }

        private sealed class IncomingHandler : Handler
        {
            private readonly PosSystemApiRequestHandler _handler;

            public IncomingHandler(Looper looper, PosSystemApiRequestHandler handler) : base(looper)
            {
                _handler = handler;
            }

            public override void HandleMessage(Message msg)
            {
                if (msg.What != PosSystemApiServiceContract.MsgRequest)
                {
                    Log.Warn(TAG, $"Ignoring unknown message.What={msg.What}");
                    return;
                }

                var replyTo = msg.ReplyTo;
                var data = msg.Data;

                if (replyTo == null)
                {
                    Log.Warn(TAG, "No ReplyTo Messenger set on request; dropping response.");
                    return;
                }
                _ = Task.Run(async () =>
                {
                    PosSystemApiResponse response;
                    try
                    {
                        var request = ParseRequest(data);
                        response = await _handler.HandleAsync(request).ConfigureAwait(false);
                    }
                    catch (ArgumentException ex)
                    {
                        Log.Error(TAG, $"Invalid request: {ex.Message}");
                        response = PosSystemApiResponse.Error(400, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(TAG, $"Unhandled error while processing request: {ex}");
                        response = PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}");
                    }

                    SendReply(replyTo, response);
                });
            }

            private static PosSystemApiRequest ParseRequest(Bundle? data)
            {
                if (data == null)
                    throw new ArgumentException("Message.Data is required");

                var method = data.GetString(PosSystemApiServiceContract.KeyMethod);
                var path = data.GetString(PosSystemApiServiceContract.KeyPath);
                var headerBase64Url = data.GetString(PosSystemApiServiceContract.KeyHeaderJsonBase64Url);
                var bodyBase64Url = data.GetString(PosSystemApiServiceContract.KeyBodyBase64Url);

                return PosSystemApiRequestExtensions.Parse(method, path, headerBase64Url, bodyBase64Url);
            }

            private static void SendReply(Messenger? replyTo, PosSystemApiResponse response)
            {
                try
                {
                    var reply = Message.Obtain();
                    reply.What = PosSystemApiServiceContract.MsgReply;

                    var bundle = new Bundle();

                    var intentData = Extensions.PosSystemApiResponseExtensions.ToIntentData(response);
                    bundle.PutString(PosSystemApiServiceContract.KeyStatusCode, intentData.StatusCode);
                    bundle.PutString(PosSystemApiServiceContract.KeyContentBase64Url, intentData.ContentBase64Url);
                    bundle.PutString(PosSystemApiServiceContract.KeyContentTypeBase64Url, intentData.ContentTypeBase64Url);
                    if (!string.IsNullOrEmpty(intentData.HeadersBase64Url))
                        bundle.PutString(PosSystemApiServiceContract.KeyResponseHeaderJsonBase64Url, intentData.HeadersBase64Url);

                    reply.Data = bundle;

                    replyTo.Send(reply);
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to send reply: {ex}");
                }
            }
        }
    }
}
