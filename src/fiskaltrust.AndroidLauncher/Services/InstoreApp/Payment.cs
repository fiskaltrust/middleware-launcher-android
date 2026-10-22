using fiskaltrust.Api.PosSystem.Core.v2.Pay.Models;
using fiskaltrust.ifPOS.v2;
using System.Text.Json.Serialization;
namespace fiskaltrust.Payment;

public record PayResponseRequest(string operationId, int lifetime);

public record PayRequestAcceptedResponse(string operationId, Guid queueId, Guid queueItemId);
public record PayRequestNotAcceptedResponse(string operationId, PayRequestNotAcceptedReason reason);
public record PayRequestNotAcceptedReason(NotAcceptedReasonCode code, string? message);

public enum States
{
    [JsonPropertyName("unknown")]
    unknown,
    [JsonPropertyName("done")]
    done,
    [JsonPropertyName("failed")]
    failed
}
[JsonConverter(typeof(JsonStringEnumConverter<NotAcceptedReasonCode>))]
public enum NotAcceptedReasonCode
{
    /// <summary>
    /// Read side only: a code this version does not know yet (sent by a newer InStore App). Never sent.
    /// </summary>
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    /// <summary>This device is not paired to a cashbox.</summary>
    [JsonStringEnumMemberName("not_paired")]
    NotPaired,

    /// <summary>The cashbox id / access token do not match the cashbox this device is paired to.</summary>
    [JsonStringEnumMemberName("unauthorized")]
    Unauthorized,

    /// <summary>The request is malformed (missing field or payload that is not a PaymentRequest).</summary>
    [JsonStringEnumMemberName("invalid_request")]
    InvalidRequest,

    /// <summary>No payment method is configured on this device.</summary>
    [JsonStringEnumMemberName("payment_not_configured")]
    PaymentNotConfigured,

    /// <summary>The requested payment protocol cannot be used on this device.</summary>
    [JsonStringEnumMemberName("protocol_not_supported")]
    ProtocolNotSupported,

    /// <summary>The configured payment provider does not support this request (e.g. the action).</summary>
    [JsonStringEnumMemberName("not_supported")]
    NotSupported,

    /// <summary>Unexpected failure while starting the payment, before it was accepted.</summary>
    [JsonStringEnumMemberName("internal_error")]
    InternalError
}
public class PayResponseState
{
    public string operationId { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public States state { get; set; }

    ///////////////////////////////////////////////////////
    // Error Reporting when state is failed
    //
    // https://datatracker.ietf.org/doc/html/rfc7807

    /// <summary>
    /// Will be written to the "title" field of the RFC7807 error response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? error { get; set; }

    /// <summary>
    /// Error details
    /// Will be written to the "detail" field of the RFC7807 error response.
    /// NOTE: As of today (2025-08-18) this is not yet implemented in the POS System API but we provide it now so that we can use it in the future.
    /// </summary>    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? errorDetails { get; set; }

    ///////////////////////////////////////////////////////


    public PayResponse? PaymentResponse { get; set; }

    public override string ToString()
    {
        return $"{operationId} {state} {error}";
    }
   

}

