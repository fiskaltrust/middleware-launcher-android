using Android.Content;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.ifPOS.v2;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Extensions
{
    public static class PosSystemApiRequestExtensions
    {
        public static PosSystemApiRequest FromIntent(Intent intent)
        {
            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            return Parse(
                intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_METHOD),
                intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_PATH),
                intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_HEADER_JSON_BASE64URL),
                intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_BODY_BASE64URL));
        }

        /// <summary>
        /// Validates and decodes the four raw request fields shared by the intent
        /// surface and the Messenger bundle path into a <see cref="PosSystemApiRequest"/>.
        /// Throws <see cref="ArgumentException"/> with wire-visible messages on
        /// missing or malformed input.
        /// </summary>
        public static PosSystemApiRequest Parse(string? method, string? path, string? headerBase64Url, string? bodyBase64Url)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is required");

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required");

            if (string.IsNullOrEmpty(headerBase64Url))
                throw new ArgumentException("HeaderJsonObjectBase64Url is required");

            // Decode headers
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

            // Decode body if present
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
                Body = body
            };
        }

        public static bool IsLocalEndpoint(this PosSystemApiRequest request, HashSet<string> localEndpoints)
        {
            return localEndpoints.Contains(request.Path);
        }
    }

    public static class PosSystemApiResponseExtensions
    {
        /// <summary>
        /// Encodes the response data for use in an Android Intent
        /// </summary>
        /// <param name="response">The response to encode</param>
        /// <returns>An object containing Base64URL-encoded response data</returns>
        public static IntentResponseData ToIntentData(this PosSystemApiResponse response)
        {
            string contentBase64Url=string.Empty;
            switch (response.Content)
            {
                case ResponseBody.Text text:
                    contentBase64Url = Base64UrlHelper.Encode(text.Value);
                    break;

                case ResponseBody.File file:
                    contentBase64Url = Base64UrlHelper.EncodeBytes(file.Content);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported response body type: {response.Content.GetType().Name}");
            }
            var contentTypeBase64Url = Base64UrlHelper.Encode(response.ContentType);
            var headersJson = JsonConvert.SerializeObject(response.Headers);
            var headersBase64Url = Base64UrlHelper.Encode(headersJson);

            return new IntentResponseData
            {
                StatusCode = response.StatusCode.ToString(),
                ContentBase64Url = contentBase64Url,
                ContentTypeBase64Url = contentTypeBase64Url,
                HeadersBase64Url = headersBase64Url
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
                StatusCode = (int)response.StatusCode,
                Content = new ResponseBody.Text(responseContent),
                ContentType = responseContentType,
                Headers = responseHeaders
            };
        }

    }
    public class IntentResponseData
    {
        /// <summary>
        /// HTTP status code as string
        /// </summary>
        public string StatusCode { get; set; } = string.Empty;

        /// <summary>
        /// Base64URL-encoded response content
        /// </summary>
        public string ContentBase64Url { get; set; } = string.Empty;

        /// <summary>
        /// Base64URL-encoded content type
        /// </summary>
        public string ContentTypeBase64Url { get; set; } = string.Empty;

        /// <summary>
        /// Base64URL-encoded response headers JSON
        /// </summary>
        public string HeadersBase64Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Constants for PosSystemAPI Activity Intent extras
    /// </summary>
    public static class PosSystemAPIActivityIntentStatics
    {
        // HTTP method ("POST", "GET", "PUT", "DELETE")
        public const string EXTRA_METHOD = "Method";
        // API path (e.g., "/echo", "/sign", "/pay" for latest version; e.g., "/v2/echo", "/v2/sign", "/v2/pay" for v2 specific)
        public const string EXTRA_PATH = "Path";
        // Base64URL-encoded JSON headers object (includes authentication, content-type, etc.)
        public const string EXTRA_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";
        // Base64URL-encoded request body
        public const string EXTRA_BODY_BASE64URL = "BodyBase64Url";

        // HTTP status code as string (e.g., "200", "201", "400", "500")
        public const string EXTRA_STATUS_CODE = "StatusCode";
        // Base64URL-encoded response body
        public const string EXTRA_CONTENT_BASE64URL = "ContentBase64Url";
        // Base64URL-encoded content type (e.g., "application/json")
        public const string EXTRA_CONTENT_TYPE_BASE64URL = "ContentTypeBase64Url";
        // Base64URL-encoded JSON headers object (optional, for response headers)
        public const string EXTRA_RESPONSE_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";
    }
}
