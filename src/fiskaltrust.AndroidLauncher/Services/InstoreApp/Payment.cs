using fiskaltrust.Api.PosSystem.Core.v2.Pay.Models;
using fiskaltrust.ifPOS.v2;
using System.Text.Json.Serialization;

namespace fiskaltrust.Payment;

public record PayResponseRequest(string operationId, int lifetime);

public record PayRequestAcceptedResponse(string operationId, Guid queueId, Guid queueItemId);

public enum States
{
    [JsonPropertyName("unknown")]
    unknown,
    [JsonPropertyName("done")]
    done,
    [JsonPropertyName("failed")]
    failed
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

