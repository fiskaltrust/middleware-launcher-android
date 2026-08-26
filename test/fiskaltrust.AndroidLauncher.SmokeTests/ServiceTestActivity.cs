using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    /// <summary>
    /// Calls the launcher's PosSystemAPIService over the bound Messenger interface.
    /// Launching the activity runs a single request and logs the result to logcat;
    /// <see cref="TestInstrumentation"/> uses <see cref="SendRequest"/> directly.
    /// </summary>
    [Activity(
        Name = "eu.fiskaltrust.androidlauncher.smoketests.ServiceTestActivity",
        Exported = true)]
    public class ServiceTestActivity : Activity
    {
        private const string TAG = "ServiceTest";

        private const string LauncherPackage = "eu.fiskaltrust.androidlauncher";
        private const string ServiceClass = "eu.fiskaltrust.androidlauncher.PosSystemAPIService";

        private const int MsgRequest = 1;
        private const int MsgReply = 2;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var request = PosSystemApiRequest.FromExtras(name => Intent?.GetStringExtra(name));
            Task.Run(() => RunTest(request));
        }

        private void RunTest(PosSystemApiRequest request)
        {
            try
            {
                var statusCode = SendRequest(this, request);
                Log.Info(TAG, $"RESULT={statusCode}");
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"RESULT=ERROR:{ex.Message}");
            }
            finally
            {
                RunOnUiThread(Finish);
            }
        }

        /// <summary>
        /// Sends a single request to the launcher's PosSystemAPIService over the bound
        /// Messenger interface and waits synchronously for the reply. Returns the
        /// StatusCode from the reply; throws with a descriptive message on binding
        /// failures, timeouts, or a reply without a StatusCode.
        /// </summary>
        internal static string SendRequest(Context context, PosSystemApiRequest request)
        {
            var connection = new ServiceConnection();
            HandlerThread? replyThread = null;
            var bound = false;

            try
            {
                var intent = new Intent();
                intent.SetClassName(LauncherPackage, ServiceClass);

                bound = context.BindService(intent, connection, Bind.AutoCreate);
                if (!bound)
                {
                    throw new InvalidOperationException("BindService returned false");
                }

                if (!connection.Connected.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("Timed out waiting for OnServiceConnected");
                }

                replyThread = new HandlerThread("ServiceTestReplyThread");
                replyThread.Start();

                var replyHandler = new ReplyHandler(replyThread.Looper!);
                var replyMessenger = new Messenger(replyHandler);
                var serviceMessenger = new Messenger(connection.Binder!);

                var message = Message.Obtain()!;
                message.What = MsgRequest;
                message.ReplyTo = replyMessenger;

                var data = new Bundle();
                request.WriteTo(data);
                message.Data = data;

                serviceMessenger.Send(message);

                if (!replyHandler.Received.Wait(TimeSpan.FromMinutes(5)))
                {
                    throw new TimeoutException($"Timed out waiting for reply from PosSystemAPIService after 5min");
                }

                return replyHandler.ReplyData?.GetString(PosSystemApiRequest.KeyStatusCode)
                    ?? throw new InvalidOperationException("No StatusCode in reply");
            }
            finally
            {
                if (bound)
                {
                    try
                    {
                        context.UnbindService(connection);
                    }
                    catch (Exception)
                    {
                    }
                }
                replyThread?.QuitSafely();
            }
        }

        private sealed class ServiceConnection : Java.Lang.Object, IServiceConnection
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
