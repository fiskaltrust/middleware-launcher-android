using System.Text;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.PosSystemApiTestClient
{
    [Activity(
        Name = "eu.fiskaltrust.androidlauncher.testclient.ServiceTestActivity",
        Exported = true)]
    public class ServiceTestActivity : Activity
    {
        private const string TAG = "ServiceTest";

        private const string LauncherPackage = "eu.fiskaltrust.androidlauncher";
        private const string ServiceClass = "eu.fiskaltrust.androidlauncher.PosSystemAPIService";

        private const int MsgRequest = 1;
        private const int MsgReply = 2;

        private const string KeyMethod = "Method";
        private const string KeyPath = "Path";
        private const string KeyHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
        private const string KeyBodyBase64Url = "BodyBase64Url";
        private const string KeyStatusCode = "StatusCode";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Task.Run(RunTestAsync);
        }

        private async Task RunTestAsync()
        {
            var connection = new ServiceTestConnection();
            HandlerThread? replyThread = null;
            var bound = false;

            try
            {
                var method = Intent?.GetStringExtra(KeyMethod) ?? "POST";
                var path = Intent?.GetStringExtra(KeyPath) ?? "/v2/echo";
                var headerJsonBase64Url = Intent?.GetStringExtra(KeyHeaderJsonBase64Url) ?? DefaultHeadersBase64Url();
                var bodyBase64Url = Intent?.GetStringExtra(KeyBodyBase64Url) ?? ToBase64Url(JsonSerializer.Serialize(new { Message = "Ping" }));

                var intent = new Intent();
                intent.SetClassName(LauncherPackage, ServiceClass);

                bound = BindService(intent, connection, Bind.AutoCreate);
                if (!bound)
                {
                    Log.Info(TAG, "RESULT=ERROR:BindService returned false");
                    Finish();
                    return;
                }

                if (!connection.Connected.Wait(TimeSpan.FromSeconds(30)))
                {
                    Log.Info(TAG, "RESULT=ERROR:Timed out waiting for OnServiceConnected");
                    Finish();
                    return;
                }

                replyThread = new HandlerThread("ServiceTestReplyThread");
                replyThread.Start();

                var replyHandler = new ReplyHandler(replyThread.Looper!);
                var replyMessenger = new Messenger(replyHandler);
                var serviceMessenger = new Messenger(connection.Binder!);

                var request = Message.Obtain()!;
                request.What = MsgRequest;
                request.ReplyTo = replyMessenger;

                var data = new Bundle();
                data.PutString(KeyMethod, method);
                data.PutString(KeyPath, path);
                data.PutString(KeyHeaderJsonBase64Url, headerJsonBase64Url);
                data.PutString(KeyBodyBase64Url, bodyBase64Url);
                request.Data = data;

                serviceMessenger.Send(request);

                if (!replyHandler.Received.Wait(TimeSpan.FromMinutes(5)))
                {
                    Log.Info(TAG, "RESULT=ERROR:Timed out waiting for reply from PosSystemAPIService");
                    Finish();
                    return;
                }

                var statusCode = replyHandler.ReplyData?.GetString(KeyStatusCode);
                Log.Info(TAG, $"RESULT={statusCode ?? "ERROR:no StatusCode in reply"}");
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"RESULT=ERROR:Unhandled exception: {ex}");
            }
            finally
            {
                if (bound)
                {
                    try
                    {
                        UnbindService(connection);
                    }
                    catch (Exception)
                    {
                    }
                }
                replyThread?.QuitSafely();
                Finish();
            }
        }

        private static string DefaultHeadersBase64Url()
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "x-cashbox-id", TestConstants.Http.CashboxId },
                { "x-cashbox-accesstoken", TestConstants.Http.AccessToken },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };
            return ToBase64Url(JsonSerializer.Serialize(headers));
        }

        private static string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private sealed class ServiceTestConnection : Java.Lang.Object, IServiceConnection
        {
            public ManualResetEventSlim Connected { get; } = new ManualResetEventSlim(false);
            public IBinder? Binder { get; private set; }

            public void OnServiceConnected(ComponentName? name, IBinder? service)
            {
                Binder = service;
                Connected.Set();
            }

            public void OnServiceDisconnected(ComponentName? name)
            {
            }

            public void OnBindingDied(ComponentName? name)
            {
            }

            public void OnNullBinding(ComponentName? name)
            {
            }
        }

        private sealed class ReplyHandler : Handler
        {
            public ManualResetEventSlim Received { get; } = new ManualResetEventSlim(false);
            public Bundle? ReplyData { get; private set; }

            public ReplyHandler(Looper looper) : base(looper)
            {
            }

            public override void HandleMessage(Message msg)
            {
                if (msg.What == MsgReply)
                {
                    ReplyData = msg.Data;
                    Received.Set();
                }
            }
        }
    }
}
