using Android.Content;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.Api.PosSystemLocal.Models;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Extensions
{
    /// <summary>
    /// Android-specific extensions for PosSystemApiRequest
    /// </summary>
    public static class PosSystemApiRequestExtensions
    {
        /// <summary>
        /// Creates a PosSystemApiRequest from an Android Intent
        /// </summary>
        /// <param name="intent">The intent containing the request data</param>
        /// <returns>A parsed PosSystemApiRequest</returns>
        /// <exception cref="ArgumentException">Thrown when required intent extras are missing or invalid</exception>
        public static PosSystemApiRequest FromIntent(Intent intent)
        {
            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            // Extract required fields
            var method = intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_METHOD);
            var path = intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_PATH);
            var headerBase64Url = intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_HEADER_JSON_BASE64URL);
            var bodyBase64Url = intent.GetStringExtra(PosSystemAPIActivityIntentStatics.EXTRA_BODY_BASE64URL);

            // Validate required fields
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is required", nameof(intent));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required", nameof(intent));

            if (string.IsNullOrEmpty(headerBase64Url))
                throw new ArgumentException("HeaderJsonObjectBase64Url is required", nameof(intent));

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
                throw new ArgumentException($"Invalid headers format: {ex.Message}", nameof(intent), ex);
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
                    throw new ArgumentException($"Invalid body format: {ex.Message}", nameof(intent), ex);
                }
            }

            return PosSystemApiRequest.Create(method, path, headers, body);
        }
    }

    /// <summary>
    /// Android-specific extensions for PosSystemApiResponse
    /// </summary>
    public static class PosSystemApiResponseExtensions
    {
        /// <summary>
        /// Encodes the response data for use in an Android Intent
        /// </summary>
        /// <param name="response">The response to encode</param>
        /// <returns>An object containing Base64URL-encoded response data</returns>
        public static IntentResponseData ToIntentData(this PosSystemApiResponse response)
        {
            var contentBase64Url = Base64UrlHelper.Encode(response.Content);
            var contentTypeBase64Url = Base64UrlHelper.Encode(response.ContentType);
            var headersJson = JsonConvert.SerializeObject(response.Headers);
            var headersBase64Url = Base64UrlHelper.Encode(headersJson);

            return new IntentResponseData
            {
                StatusCode = response.StatusCode,
                ContentBase64Url = contentBase64Url,
                ContentTypeBase64Url = contentTypeBase64Url,
                HeadersBase64Url = headersBase64Url
            };
        }

        /// <summary>
        /// Creates a PosSystemApiResponse from Base64URL-encoded intent data
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="contentBase64Url">Base64URL-encoded content</param>
        /// <param name="contentTypeBase64Url">Base64URL-encoded content type</param>
        /// <param name="headersBase64Url">Base64URL-encoded headers JSON (optional)</param>
        /// <returns>A decoded PosSystemApiResponse</returns>
        public static PosSystemApiResponse FromIntentData(
            string statusCode,
            string contentBase64Url,
            string contentTypeBase64Url,
            string? headersBase64Url = null)
        {
            var content = Base64UrlHelper.Decode(contentBase64Url);
            var contentType = Base64UrlHelper.Decode(contentTypeBase64Url);

            string? headersJson = null;
            if (!string.IsNullOrEmpty(headersBase64Url))
            {
                try
                {
                    headersJson = Base64UrlHelper.Decode(headersBase64Url);
                }
                catch (Exception)
                {
                    // If headers can't be decoded, continue with null
                }
            }

            return PosSystemApiResponse.FromData(statusCode, content, contentType, headersJson);
        }
    }

    /// <summary>
    /// Represents Base64URL-encoded response data for Android Intent extras
    /// </summary>
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