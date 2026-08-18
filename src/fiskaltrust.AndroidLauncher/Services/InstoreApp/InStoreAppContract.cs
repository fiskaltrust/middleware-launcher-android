namespace fiskaltrust.AndroidLauncher.Services.InStoreApp
{
    /// <summary>
    /// The wire contract for local (on-device) payment communication with the InStore App.
    /// Concept: docs/okf/communication/transportAndroid.md
    ///
    /// The caller binds the InStore App's exported Messenger service and exchanges Messages with it. The
    /// InStore App answers to the Messenger supplied in Message.ReplyTo.
    /// </summary>
    public static class InStoreAppService
    {
        /// <summary>Package of the InStore App hosting the bound service.</summary>
        public const string Package = "eu.fiskaltrust.instore.app";

        /// <summary>
        /// Fully qualified class name of the exported bound service.
        /// NOTE: .NET for Android derives this from the [Service] attribute - it must stay in sync with
        /// the Name set on InStoreAppService in the InStore App.
        /// </summary>
        public const string ServiceClass = "eu.fiskaltrust.instore.app.LocalCommService";
    }

    /// <summary>
    /// Values for Message.What. This is a FIXED contract: unlike an MQTT topic an int is not
    /// self-describing, so a mismatch between caller and app would silently misinterpret a message
    /// instead of falling into an "unhandled" branch. Numbered in message-sequence order.
    ///
    /// The numbers are what travels over the wire and must never change once shipped. The member names keep
    /// the MSG_ wire-contract spelling on purpose (rather than C# PascalCase) so they read the same on both
    /// sides of the channel, including a non-.NET caller.
    ///
    /// An arbitrary int off the wire is turned into one of these by
    /// <see cref="InStoreAppEnvelopeCodec.TryUnpack"/>, which rejects anything unknown - so everything past
    /// that boundary works with this type rather than a raw int.
    /// </summary>
    public enum InStoreAppMessages
    {
        /// <summary>caller -> app: a PaymentRequest.</summary>
        MSG_PAY_REQUEST = 1,

        /// <summary>app -> caller: a PayRequestAcceptedResponse.</summary>
        MSG_PAY_REQUEST_ACCEPTED = 2,

        /// <summary>caller -> app: a PayResponseRequest (the poll used to recover an undelivered result).</summary>
        MSG_PAY_RESPONSE_REQUEST = 3,

        /// <summary>app -> caller: a PayResponseState (final or still in progress).</summary>
        MSG_PAY_RESPONSE_STATE = 4
    }

    public static class InStoreAppMessagesExtensions
    {
        /// <summary>
        /// True for the caller -> app messages. Only those carry the cashbox identity, because only those have
        /// to be authenticated: the InStore App checks that a request is meant for the cashbox it is paired to.
        ///
        /// The replies (app -> caller) deliberately carry no identity - the access token is a credential the app
        /// must not hand out, and a reply is correlated by its operationId instead.
        /// </summary>
        public static bool IsFromCaller(this InStoreAppMessages what)
            => what == InStoreAppMessages.MSG_PAY_REQUEST || what == InStoreAppMessages.MSG_PAY_RESPONSE_REQUEST;
    }

    /// <summary>Helpers for putting InStoreApp messages into a log without flooding it.</summary>
    public static class InStoreAppLogging
    {
        /// <summary>
        /// Max payload characters to log. A PayResponseState carries the full PaymentResponse including the
        /// complete card receipt, which runs into thousands of characters - well past logcat's ~4000 byte
        /// per line limit, so it would be truncated mid-JSON anyway while flooding the log and putting
        /// receipt details (masked PAN, auth code, terminal and merchant ids) into the device log.
        /// </summary>
        public const int MaxLoggedPayloadChars = 512;

        /// <summary>Shorten a payload for logging, noting how much was left out.</summary>
        public static string ForLog(string? payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson)) return "";
            if (payloadJson!.Length <= MaxLoggedPayloadChars) return payloadJson;

            return payloadJson.Substring(0, MaxLoggedPayloadChars)
                   + $"... [truncated, {payloadJson.Length} chars total]";
        }
    }

    /// <summary>Keys used inside Message.Data.</summary>
    public static class InStoreAppKeys
    {
        /// <summary>The operation this message belongs to. Always present.</summary>
        public const string OperationId = "operationId";

        /// <summary>Id of the cashbox the message is meant for. Always present; validated by the app.</summary>
        public const string CashboxId = "cashboxId";

        /// <summary>Access token of that cashbox. Always present; validated by the app.</summary>
        public const string AccessToken = "accessToken";

        /// <summary>The message itself, serialized as a JSON string.</summary>
        public const string Payload = "payload";
    }
}
