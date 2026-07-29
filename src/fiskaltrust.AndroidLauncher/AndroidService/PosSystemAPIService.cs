using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.Api.PosSystem.Core.Models;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.AndroidService
{    
    [Service(
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPIService",
        Exported = true,
        Permission = PosSystemApiServiceContract.Permission)]
    public class PosSystemAPIService : Service
    {
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

            _requestHandler = new PosSystemApiRequestHandler();
            _messenger = new Messenger(new IncomingHandler(_handlerThread.Looper!, _requestHandler));
        }

        public override IBinder? OnBind(Intent? intent)
        {
            Log.Info(TAG, "OnBind");
            return _messenger?.Binder;
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
            base.OnDestroy();
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
                var correlationId = data?.GetString(PosSystemApiServiceContract.KeyCorrelationId);
                
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

                    SendReply(replyTo, correlationId, response);
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

                if (string.IsNullOrEmpty(method))
                    throw new ArgumentException("Method is required");
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path is required");
                if (string.IsNullOrEmpty(headerBase64Url))
                    throw new ArgumentException("HeaderJsonObjectBase64Url is required");

                Dictionary<string, string> headers;
                try
                {
                    var headersJson = Base64UrlHelper.Decode(headerBase64Url);
                    headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson)
                        ?? new Dictionary<string, string>();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Invalid headers format: {ex.Message}", ex);
                }
                
                string? body = null;
                if (!string.IsNullOrEmpty(bodyBase64Url))
                {
                    try
                    {
                        body = Base64UrlHelper.Decode(bodyBase64Url);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Invalid body format: {ex.Message}", ex);
                    }
                }

                return new PosSystemApiRequest
                {
                    Method = method!,
                    Path = path!,
                    Headers = headers,
                    Body = body,
                };
            }

            private static void SendReply(Messenger? replyTo, string? correlationId, PosSystemApiResponse response)
            {
                try
                {
                    var reply = Message.Obtain();
                    reply.What = PosSystemApiServiceContract.MsgReply;

                    var bundle = new Bundle();
                    if (!string.IsNullOrEmpty(correlationId))
                        bundle.PutString(PosSystemApiServiceContract.KeyCorrelationId, correlationId);

                    var intentData = Extensions.PosSystemApiResponseExtensions.ToIntentData(response);
                    bundle.PutString(PosSystemApiServiceContract.KeyStatusCode, intentData.StatusCode);
                    bundle.PutString(PosSystemApiServiceContract.KeyContentBase64Url, intentData.ContentBase64Url);
                    bundle.PutString(PosSystemApiServiceContract.KeyContentTypeBase64Url, intentData.ContentTypeBase64Url);
                    if (!string.IsNullOrEmpty(intentData.HeadersBase64Url))
                        bundle.PutString(PosSystemApiServiceContract.KeyResponseHeaderJsonBase64Url, intentData.HeadersBase64Url);

                    reply.Data = bundle;

                    replyTo.Send(reply);
                    Log.Info(TAG, $"Replied {response.StatusCode} (correlationId={correlationId})");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to send reply: {ex}");
                }
            }
        }
    }
}
