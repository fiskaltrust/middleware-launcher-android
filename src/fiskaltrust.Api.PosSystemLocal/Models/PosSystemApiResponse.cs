using System.Text.Json;

namespace fiskaltrust.Api.PosSystemLocal.Models
{
    /// <summary>
    /// Represents a POS System API response with status code, content, and headers
    /// </summary>
    public class PosSystemApiResponse
    {
        /// <summary>
        /// HTTP status code as string (e.g., "200", "201", "400", "500")
        /// </summary>
        public string StatusCode { get; set; } = string.Empty;

        /// <summary>
        /// Response body content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Content type of the response (e.g., "application/json")
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Response headers as key-value pairs
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Timestamp when the response was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Determines if the response represents a successful HTTP status
        /// </summary>
        public bool IsSuccess => StatusCode.StartsWith("2");

        /// <summary>
        /// Gets the status code as an integer
        /// </summary>
        public int StatusCodeInt => int.TryParse(StatusCode, out var code) ? code : 0;

        /// <summary>
        /// Creates a successful PosSystemApiResponse
        /// </summary>
        /// <param name="content">Response content</param>
        /// <param name="contentType">Content type (defaults to application/json)</param>
        /// <param name="statusCode">HTTP status code (defaults to 200)</param>
        /// <param name="headers">Additional response headers</param>
        /// <returns>A successful PosSystemApiResponse</returns>
        public static PosSystemApiResponse Success(
            string content, 
            string contentType = "application/json", 
            string statusCode = "200",
            Dictionary<string, string>? headers = null)
        {
            return new PosSystemApiResponse
            {
                StatusCode = statusCode,
                Content = content,
                ContentType = contentType,
                Headers = headers ?? new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Creates an error PosSystemApiResponse using RFC 9110 problem details format
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="errorTitle">Error title (defaults to "Error")</param>
        /// <returns>An error PosSystemApiResponse</returns>
        public static PosSystemApiResponse Error(int statusCode, string errorMessage, string errorTitle = "Error")
        {
            var errorResponse = new
            {
                type = "https://tools.ietf.org/html/rfc9110",
                title = errorTitle,
                status = statusCode,
                detail = errorMessage
            };

            var errorJson = JsonSerializer.Serialize(errorResponse);

            return new PosSystemApiResponse
            {
                StatusCode = statusCode.ToString(),
                Content = errorJson,
                ContentType = "application/json",
                Headers = new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Creates a PosSystemApiResponse from an HTTP response
        /// </summary>
        /// <param name="response">The HTTP response message</param>
        /// <returns>A PosSystemApiResponse representing the HTTP response</returns>
        public static async Task<PosSystemApiResponse> FromHttpResponseAsync(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            // Build response headers dictionary
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            return new PosSystemApiResponse
            {
                StatusCode = ((int)response.StatusCode).ToString(),
                Content = responseContent,
                ContentType = responseContentType,
                Headers = responseHeaders
            };
        }

        /// <summary>
        /// Serializes the response headers to JSON
        /// </summary>
        /// <returns>JSON representation of the headers</returns>
        public string SerializeHeaders()
        {
            return JsonSerializer.Serialize(Headers);
        }

        /// <summary>
        /// Creates a response with deserialized headers from JSON
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="content">Response content</param>
        /// <param name="contentType">Content type</param>
        /// <param name="headersJson">JSON-encoded headers (optional)</param>
        /// <returns>A new PosSystemApiResponse</returns>
        public static PosSystemApiResponse FromData(
            string statusCode,
            string content,
            string contentType,
            string? headersJson = null)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(headersJson))
            {
                try
                {
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson)
                        ?? new Dictionary<string, string>();
                }
                catch (JsonException)
                {
                    // If headers can't be decoded, continue with empty headers
                }
            }

            return new PosSystemApiResponse
            {
                StatusCode = statusCode,
                Content = content,
                ContentType = contentType,
                Headers = headers
            };
        }
    }
}