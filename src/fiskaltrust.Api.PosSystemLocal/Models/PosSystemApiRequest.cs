using System.Text.Json;

namespace fiskaltrust.Api.PosSystemLocal.Models
{
    /// <summary>
    /// Represents a POS System API request with HTTP method, path, headers, and body
    /// </summary>
    public class PosSystemApiRequest
    {
        /// <summary>
        /// HTTP method (POST, GET, PUT, DELETE)
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// API path (e.g., "/echo", "/sign", "/v2/sign")
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Request headers as key-value pairs
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body content
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets the normalized path (ensures it starts with "/")
        /// </summary>
        public string NormalizedPath => Path.StartsWith("/") ? Path : "/" + Path;

        /// <summary>
        /// Gets the cashbox ID from headers
        /// </summary>
        public Guid CashBoxId => Guid.TryParse(Headers.GetValueOrDefault("x-cashbox-id"), out var id) ? id : Guid.Empty;

        /// <summary>
        /// Gets the access token from headers
        /// </summary>
        public string AccessToken => Headers.GetValueOrDefault("x-cashbox-accesstoken", string.Empty);

        /// <summary>
        /// Gets the operation ID from headers for idempotency
        /// </summary>
        public string? OperationId => Headers.GetValueOrDefault("x-operation-id");

        /// <summary>
        /// Determines if this is a sandbox request
        /// </summary>
        public bool IsSandbox => Headers.TryGetValue("x-sandbox", out var sandbox) && 
                                (sandbox == "true" || sandbox == "1");

        /// <summary>
        /// Validates the request for version compatibility
        /// </summary>
        /// <returns>True if the request is valid, false otherwise</returns>
        public bool IsValidVersion()
        {
            return !IsUnsupportedVersion(NormalizedPath);
        }

        /// <summary>
        /// Checks if the path represents an unsupported API version
        /// </summary>
        /// <param name="path">The API path to check</param>
        /// <returns>True if the version is unsupported</returns>
        private static bool IsUnsupportedVersion(string path)
        {
            // Check if path starts with unsupported version prefixes
            // We only support defaults (no version) and /v2
            // Reject /v0, /v1, /json/v0, /json/v1, /xml/v0, /xml/v1
            var unsupportedPrefixes = new[] { "/v0/", "/v1/", "/json/v0/", "/json/v1/", "/xml/v0/", "/xml/v1/" };

            foreach (var prefix in unsupportedPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if this request should be handled by local middleware
        /// </summary>
        /// <param name="localEndpoints">Set of endpoints that should be handled locally</param>
        /// <returns>True if this is a local endpoint</returns>
        public bool IsLocalEndpoint(HashSet<string> localEndpoints)
        {
            return localEndpoints.Contains(NormalizedPath);
        }

        /// <summary>
        /// Creates a PosSystemApiRequest from HTTP request data
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="path">API path</param>
        /// <param name="headers">Request headers</param>
        /// <param name="body">Request body (optional)</param>
        /// <returns>A new PosSystemApiRequest instance</returns>
        /// <exception cref="ArgumentException">Thrown when required parameters are missing or invalid</exception>
        public static PosSystemApiRequest Create(
            string method,
            string path,
            Dictionary<string, string> headers,
            string? body = null)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is required", nameof(method));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required", nameof(path));

            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            // Validate required headers
            if (!headers.ContainsKey("x-operation-id"))
                throw new ArgumentException("The required header x-operation-id was not sent.", nameof(headers));

            return new PosSystemApiRequest
            {
                Method = method,
                Path = path,
                Headers = headers,
                Body = body
            };
        }

        /// <summary>
        /// Creates a PosSystemApiRequest from JSON-encoded headers
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="path">API path</param>
        /// <param name="headersJson">JSON-encoded headers</param>
        /// <param name="body">Request body (optional)</param>
        /// <returns>A new PosSystemApiRequest instance</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        public static PosSystemApiRequest FromJson(
            string method,
            string path,
            string headersJson,
            string? body = null)
        {
            Dictionary<string, string> headers;
            try
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson)
                    ?? new Dictionary<string, string>();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid headers JSON format: {ex.Message}", nameof(headersJson), ex);
            }

            return Create(method, path, headers, body);
        }
    }
}