using Android.OS;

namespace fiskaltrust.AndroidLauncher.Services.InStoreApp
{
    /// <summary>
    /// One inbound/outbound local communication message, independent of the payment DTOs: the payload stays
    /// an opaque JSON string so this contract does not have to know the payment types.
    ///
    /// <see cref="ForRequest"/> / <see cref="ForReply"/> are the only way to create one - they make the access
    /// token rule explicit, so the constructor is private.
    ///
    /// NOTE: that private constructor is why this is not a positional record. C# has no syntax to restrict the
    /// accessibility of a positional record's primary constructor, so as a positional record the rule could
    /// only have been documented instead of enforced.
    /// </summary>
    public record InStoreAppEnvelope
    {
        private InStoreAppEnvelope(InStoreAppMessages what, string operationId, Guid? cashboxId, string? accessToken, string payloadJson)
        {
            What = what;
            OperationId = operationId;
            CashboxId = cashboxId;
            AccessToken = accessToken;
            PayloadJson = payloadJson;
        }

        /// <summary>the message type, see <see cref="InStoreAppMessages"/></summary>
        public InStoreAppMessages What { get; }

        /// <summary>
        /// The operation this message belongs to; the only correlation a reply needs.
        /// A GUID in its string form (validated while unpacking) - kept as a string because that is what the
        /// payment state machine takes, but it is never free-form text: the state machine parses it as a GUID
        /// and uses it as the directory name it persists the operation under.
        /// </summary>
        public string OperationId { get; }

        /// <summary>
        /// The cashbox the request is meant for; a valid GUID (validated while unpacking).
        /// Null on replies - see <see cref="AccessToken"/>.
        /// </summary>
        public Guid? CashboxId { get; }

        /// <summary>
        /// Access token of that cashbox. Null on replies.
        ///
        /// The rule: caller -> app messages carry the identity (cashbox id + access token) because they have to be
        /// authenticated; replies carry none. A reply goes to the live ReplyTo the caller supplied and is
        /// correlated by its operationId, so repeating the identity would only mean handing a credential back out.
        /// See <see cref="InStoreAppMessages.IsFromCaller"/>.
        /// </summary>
        public string? AccessToken { get; }

        /// <summary>the message itself as JSON</summary>
        public string PayloadJson { get; }

        /// <summary>A caller -> app message; carries the identity so the app can authenticate it.</summary>
        public static InStoreAppEnvelope ForRequest(InStoreAppMessages what, string operationId, Guid cashboxId, string accessToken, string payloadJson)
            => new(what, operationId, cashboxId, accessToken, payloadJson);

        /// <summary>An app -> caller message; carries no identity at all, only the operation it answers.</summary>
        public static InStoreAppEnvelope ForReply(InStoreAppMessages what, string operationId, string payloadJson)
            => new(what, operationId, null, null, payloadJson);
    }

    /// <summary>
    /// Packs / unpacks a <see cref="InStoreAppEnvelope"/> into an Android <see cref="Bundle"/>.
    /// Used by both sides so the two can never drift apart.
    /// </summary>
    public static class InStoreAppEnvelopeCodec
    {
        /// <summary>
        /// Build the Message.Data bundle for the given envelope.
        /// The identity keys are only written when the envelope actually has them (i.e. never on replies).
        /// </summary>
        public static Bundle Pack(InStoreAppEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            var bundle = new Bundle();
            bundle.PutString(InStoreAppKeys.OperationId, envelope.OperationId);
            if (envelope.CashboxId != null)
            {
                bundle.PutString(InStoreAppKeys.CashboxId, envelope.CashboxId.Value.ToString());
            }
            if (envelope.AccessToken != null)
            {
                bundle.PutString(InStoreAppKeys.AccessToken, envelope.AccessToken);
            }
            bundle.PutString(InStoreAppKeys.Payload, envelope.PayloadJson);
            return bundle;
        }

        /// <summary>
        /// Read an envelope back from a received message.
        /// The identity (cashbox id + access token) is required for caller -> app messages and ignored on replies.
        /// </summary>
        /// <param name="rawWhat">the received Message.What - any int, validated against <see cref="InStoreAppMessages"/></param>
        /// <param name="data">the received Message.Data</param>
        /// <param name="envelope">the parsed envelope, or null when something is missing or malformed</param>
        /// <param name="error">why parsing failed (for logging)</param>
        /// <returns>true when the message is well formed</returns>
        public static bool TryUnpack(int rawWhat, Bundle? data, out InStoreAppEnvelope? envelope, out string error)
        {
            envelope = null;

            // an unknown message type means the peer speaks a different version of this contract - reject it
            // here so everything downstream can rely on a real InStoreAppMessages value
            if (!Enum.IsDefined(typeof(InStoreAppMessages), rawWhat))
            {
                error = $"unknown message type {rawWhat}";
                return false;
            }
            var what = (InStoreAppMessages)rawWhat;

            if (data == null)
            {
                error = "message has no data bundle";
                return false;
            }

            var operationId = data.GetString(InStoreAppKeys.OperationId);
            var cashboxIdRaw = data.GetString(InStoreAppKeys.CashboxId);
            var accessToken = data.GetString(InStoreAppKeys.AccessToken);
            var payload = data.GetString(InStoreAppKeys.Payload);

            if (string.IsNullOrWhiteSpace(operationId)) { error = $"missing '{InStoreAppKeys.OperationId}'"; return false; }
            if (string.IsNullOrWhiteSpace(payload)) { error = $"missing '{InStoreAppKeys.Payload}'"; return false; }

            // The operation id has to be a GUID: the payment state machine parses it as one and uses it as the
            // directory name it persists the operation under, so free-form text has no business getting past
            // this boundary. Checked for every message type - a reply is correlated by it just as a request is.
            // (As with the cashbox id below, the rejected value is not part of the error.)
            if (!Guid.TryParse(operationId, out _))
            {
                error = $"'{InStoreAppKeys.OperationId}' is not a valid GUID";
                return false;
            }

            if (what.IsFromCaller())
            {
                if (string.IsNullOrWhiteSpace(cashboxIdRaw)) { error = $"missing '{InStoreAppKeys.CashboxId}'"; return false; }
                if (string.IsNullOrWhiteSpace(accessToken)) { error = $"missing '{InStoreAppKeys.AccessToken}'"; return false; }

                // the cashbox id has to be a GUID - reject malformed input here at the boundary so everything
                // downstream can work with a real Guid instead of re-parsing a string.
                // NOTE: the rejected value is deliberately NOT part of the error. The app side logs this error
                // for a message it has not authenticated yet (anyone can bind the exported service), and
                // echoing caller supplied text would let any app write what it likes into the device log.
                if (!Guid.TryParse(cashboxIdRaw, out var cashboxId))
                {
                    error = $"'{InStoreAppKeys.CashboxId}' is not a valid GUID";
                    return false;
                }

                envelope = InStoreAppEnvelope.ForRequest(what, operationId!, cashboxId, accessToken!, payload!);
            }
            else
            {
                // a reply carries no identity; ignore any a peer may have put in
                envelope = InStoreAppEnvelope.ForReply(what, operationId!, payload!);
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Convenience: build a ready to send Message (Data set, ReplyTo left to the caller).
        /// </summary>
        public static Message ToMessage(InStoreAppEnvelope envelope)
        {
            var msg = Message.Obtain();
            msg!.What = (int)envelope.What;
            msg.Data = Pack(envelope);
            return msg;
        }
    }
}
