using Android.Content;
using Android.OS;
using Android.Util;
using System;
using System.Threading.Tasks;
using Object = Java.Lang.Object;

namespace fiskaltrust.AndroidLauncher.Services.InStoreApp
{
    /// <summary>
    /// Caller side of the local payment communication: binds the InStore App's exported Messenger service,
    /// sends messages to it and surfaces whatever the app sends back to our ReplyTo Messenger.
    /// Concept: docs/okf/communication/transportAndroid.md
    ///
    /// This lives in the contract assembly so that every caller uses the same client: the fiskaltrust Android
    /// launcher in production and the InStoreAppApp for exercising the channel by hand. The two sides of the
    /// channel can then only drift apart by changing this project.
    ///
    /// NOTE on logging: this deliberately uses <see cref="Android.Util.Log"/> instead of
    /// fiskaltrust.Utils.Logging, which would be the only dependency of this assembly. The output is the same
    /// (that logger writes to logcat with the tag and priority used here), and a shared contract is worth
    /// keeping free of dependencies its consumers would have to take on.
    /// </summary>
    public class InStoreAppClient : Object, IServiceConnection
    {
        private const string LogTag = "InStoreAppClient";

        private Messenger? _outgoing;
        private readonly Messenger _incoming;
        private TaskCompletionSource<bool>? _connectTcs;

        public InStoreAppClient()
        {
            _incoming = new Messenger(new IncomingHandler(Looper.MainLooper!, this));
        }

        public bool IsConnected => _outgoing != null;

        /// <summary>
        /// Waits for OnServiceConnected to fire - BindService() only means the bind request was accepted, the
        /// actual connection (and thus _outgoing) is set asynchronously afterwards.
        /// </summary>
        public async Task<bool> WaitForConnectionAsync()
        {
            if (IsConnected) return true;

            var completed = await Task.WhenAny(_connectTcs.Task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            return completed == _connectTcs.Task && IsConnected;
        }

        /// <summary>Raised for every message received from the InStore App.</summary>
        public event Action<InStoreAppEnvelope>? MessageReceived;

        /// <summary>Raised for connection state changes and local problems worth showing in the log.</summary>
        public event Action<string>? Log;

        public bool Bind(Context context)
        {
            Android.Util.Log.Info(LogTag, $"binding {InStoreAppService.Package}/{InStoreAppService.ServiceClass} ...");

            var intent = new Intent();
            intent.SetComponent(new ComponentName(InStoreAppService.Package, InStoreAppService.ServiceClass));

            _connectTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ok = context.BindService(intent, this, Android.Content.Bind.AutoCreate);
            if (!ok)
            {
                Report(LogPriority.Warn, "bindService returned false - is the InStore App installed? (and is the <queries> entry present?)");
            }
            return ok;
        }

        public void Unbind(Context context)
        {
            if (_outgoing == null) return;
            try
            {
                context.UnbindService(this);
            }
            catch (Java.Lang.Exception ex)
            {
                Report(LogPriority.Error, $"unbindService failed: {ex.Message}");
            }
            _outgoing = null;
            Report(LogPriority.Info, "unbound");
        }

        /// <summary>
        /// Send one message to the InStore App. Every message carries the cashbox credentials - the app only
        /// accepts it when they match the cashbox it is paired to.
        /// </summary>
        /// <param name="what">
        /// Only the caller -> app values make sense here - this client never authors a reply. Enforced below
        /// via <see cref="InStoreAppMessagesExtensions.IsFromCaller"/>, so the rule stays in one place shared
        /// with the receive side.
        /// </param>
        public bool Send(InStoreAppMessages what, string operationId, Guid cashboxId, string accessToken, string payloadJson)
        {
            if (!what.IsFromCaller())
            {
                throw new ArgumentException($"{what} is not a caller -> app message and cannot be sent by this client", nameof(what));
            }

            if (_outgoing == null)
            {
                Report(LogPriority.Warn, "cannot send - not bound");
                return false;
            }

            var envelope = InStoreAppEnvelope.ForRequest(what, operationId, cashboxId, accessToken, payloadJson);
            var msg = InStoreAppEnvelopeCodec.ToMessage(envelope);
            msg.ReplyTo = _incoming;

            try
            {
                _outgoing.Send(msg);
                Android.Util.Log.Debug(LogTag, $"--> {what} {operationId}");
                return true;
            }
            catch (Java.Lang.Exception ex)
            {
                Report(LogPriority.Error, $"send failed ({ex.GetType().Name}): {ex.Message}");
                _outgoing = null;
                return false;
            }
        }

        public void OnServiceConnected(ComponentName? name, IBinder? service)
        {
            _outgoing = service == null ? null : new Messenger(service);
            Report(LogPriority.Info, "connected to " + (name?.ClassName ?? "?"));
            _connectTcs?.TrySetResult(_outgoing != null);
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            _outgoing = null;
            Report(LogPriority.Warn, "service disconnected (InStore App process gone)");
        }

        /// <summary>
        /// Write to logcat and raise <see cref="Log"/> with the same text. A production caller has no
        /// on-screen log, so logcat is the only place this client's activity shows up; the InStoreApp wants
        /// the same message in its UI.
        /// </summary>
        private void Report(LogPriority priority, string message)
        {
            Android.Util.Log.WriteLine(priority, LogTag, message);
            Log?.Invoke(message);
        }

        private class IncomingHandler : Handler
        {
            private readonly InStoreAppClient _owner;

            public IncomingHandler(Looper looper, InStoreAppClient owner) : base(looper)
            {
                _owner = owner;
            }

            public override void HandleMessage(Message msg)
            {
                if (InStoreAppEnvelopeCodec.TryUnpack(msg.What, msg.Data, out var envelope, out var error))
                {
                    Android.Util.Log.Debug(LogTag, $"<-- {envelope!.What} {envelope.OperationId}");
                    _owner.MessageReceived?.Invoke(envelope!);
                }
                else
                {
                    _owner.Report(LogPriority.Warn, $"received unreadable message (what={msg.What}): {error}");
                }
            }
        }
    }
}
