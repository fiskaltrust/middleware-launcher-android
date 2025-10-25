using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using fiskaltrust.AndroidLauncher.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    /// <summary>
    /// Activity that handles Intent-based POS System API calls.
    /// Receives API requests via Android Intents and forwards them to the local middleware HTTP service.
    /// </summary>
    [Activity(
        Label = "PosSystemAPI",
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPI",
        LaunchMode = LaunchMode.SingleTop,
        Exported = true)]
    public class PosSystemAPIActivity : Activity
    {
        private const string TAG = "PosSystemAPI";
        
        // Default localhost URL - this should match the middleware REST endpoint configuration
        // The middleware typically runs on port 1200 for HTTP/REST endpoints
        private const string LOCALHOST_BASE_URL = "http://localhost:1200";

        // Intent extra keys for input
        private const string EXTRA_METHOD = "Method";
        private const string EXTRA_PATH = "Path";
        private const string EXTRA_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";
        private const string EXTRA_BODY_BASE64URL = "BodyBase64Url";

        // Intent extra keys for output
        private const string EXTRA_STATUS_CODE = "StatusCode";
        private const string EXTRA_CONTENT_BASE64URL = "ContentBase64Url";
        private const string EXTRA_CONTENT_TYPE_BASE64URL = "ContentTypeBase64Url";
        private const string EXTRA_RESPONSE_HEADER_JSON_BASE64URL = "HeaderJsonObjectBase64Url";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            Log.Info(TAG, "PosSystemAPI Activity started");
            
            // Process the intent asynchronously
            Task.Run(async () => await ProcessIntentAsync());
        }

        private async Task ProcessIntentAsync()
        {
            try
            {
                var intent = Intent;
                if (intent == null)
                {
                    Log.Error(TAG, "Intent is null");
                    FinishWithError(500, "Intent is null");
                    return;
                }

                // Extract intent extras
                var method = intent.GetStringExtra(EXTRA_METHOD);
                var path = intent.GetStringExtra(EXTRA_PATH);
                var headerBase64Url = intent.GetStringExtra(EXTRA_HEADER_JSON_BASE64URL);
                var bodyBase64Url = intent.GetStringExtra(EXTRA_BODY_BASE64URL);

                Log.Info(TAG, $"Processing request: {method} {path}");

                // Validate required fields
                if (string.IsNullOrEmpty(method))
                {
                    Log.Error(TAG, "Method is missing");
                    FinishWithError(400, "Method is required");
                    return;
                }

                if (string.IsNullOrEmpty(path))
                {
                    Log.Error(TAG, "Path is missing");
                    FinishWithError(400, "Path is required");
                    return;
                }

                if (string.IsNullOrEmpty(headerBase64Url))
                {
                    Log.Error(TAG, "HeaderJsonObjectBase64Url is missing");
                    FinishWithError(400, "HeaderJsonObjectBase64Url is required");
                    return;
                }

                // Decode headers
                Dictionary<string, string> headers;
                try
                {
                    var headersJson = Base64UrlHelper.Decode(headerBase64Url);
                    headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson) 
                        ?? new Dictionary<string, string>();
                    Log.Debug(TAG, $"Decoded {headers.Count} headers");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to decode headers: {ex.Message}");
                    FinishWithError(400, $"Invalid headers format: {ex.Message}");
                    return;
                }

                // Decode body if present
                string? body = null;
                if (!string.IsNullOrEmpty(bodyBase64Url))
                {
                    try
                    {
                        body = Base64UrlHelper.Decode(bodyBase64Url);
                        Log.Debug(TAG, $"Decoded body: {body.Length} characters");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(TAG, $"Failed to decode body: {ex.Message}");
                        FinishWithError(400, $"Invalid body format: {ex.Message}");
                        return;
                    }
                }

                // Make HTTP request to local middleware
                await MakeHttpRequestAsync(method, path, headers, body);
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Unexpected error: {ex}");
                FinishWithError(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task MakeHttpRequestAsync(string method, string path, Dictionary<string, string> headers, string? body)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // Allow longer timeout for operations

            try
            {
                // Ensure path starts with /
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }

                var url = LOCALHOST_BASE_URL + path;
                Log.Info(TAG, $"Making HTTP request to {url}");

                // Create HTTP request
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Add headers (skip certain headers that HttpClient handles automatically)
                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                { 
                    "Host", "Content-Length", "Connection" 
                };

                foreach (var header in headers)
                {
                    if (skipHeaders.Contains(header.Key))
                    {
                        continue;
                    }

                    try
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            // Content-Type will be set with content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(TAG, $"Failed to add header {header.Key}: {ex.Message}");
                    }
                }

                // Add body if present
                if (!string.IsNullOrEmpty(body))
                {
                    var contentType = headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                // Send request
                var response = await httpClient.SendAsync(request);
                
                Log.Info(TAG, $"Received response: {(int)response.StatusCode}");

                // Read response
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

                // Encode response
                var statusCode = ((int)response.StatusCode).ToString();
                var contentBase64Url = Base64UrlHelper.Encode(responseContent);
                var contentTypeBase64Url = Base64UrlHelper.Encode(responseContentType);
                var responseHeadersJson = JsonConvert.SerializeObject(responseHeaders);
                var responseHeadersBase64Url = Base64UrlHelper.Encode(responseHeadersJson);

                // Return result
                FinishWithResult(statusCode, contentBase64Url, contentTypeBase64Url, responseHeadersBase64Url);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(TAG, $"HTTP request failed: {ex.Message}");
                FinishWithError(502, $"Failed to communicate with middleware: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                Log.Error(TAG, $"Request timeout: {ex.Message}");
                FinishWithError(504, "Request timeout");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Request processing failed: {ex}");
                FinishWithError(500, $"Request processing failed: {ex.Message}");
            }
        }

        private void FinishWithResult(string statusCode, string contentBase64Url, string contentTypeBase64Url, string? headerBase64Url = null)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var resultIntent = new Intent();
                    resultIntent.PutExtra(EXTRA_STATUS_CODE, statusCode);
                    resultIntent.PutExtra(EXTRA_CONTENT_BASE64URL, contentBase64Url);
                    resultIntent.PutExtra(EXTRA_CONTENT_TYPE_BASE64URL, contentTypeBase64Url);
                    
                    if (!string.IsNullOrEmpty(headerBase64Url))
                    {
                        resultIntent.PutExtra(EXTRA_RESPONSE_HEADER_JSON_BASE64URL, headerBase64Url);
                    }

                    SetResult(Result.Ok, resultIntent);
                    Log.Info(TAG, $"Finishing with success: {statusCode}");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to set result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }

        private void FinishWithError(int statusCode, string errorMessage)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var errorResponse = new
                    {
                        type = "https://tools.ietf.org/html/rfc9110",
                        title = "Error",
                        status = statusCode,
                        detail = errorMessage
                    };

                    var errorJson = JsonConvert.SerializeObject(errorResponse);
                    var contentBase64Url = Base64UrlHelper.Encode(errorJson);
                    var contentTypeBase64Url = Base64UrlHelper.Encode("application/json");

                    var resultIntent = new Intent();
                    resultIntent.PutExtra(EXTRA_STATUS_CODE, statusCode.ToString());
                    resultIntent.PutExtra(EXTRA_CONTENT_BASE64URL, contentBase64Url);
                    resultIntent.PutExtra(EXTRA_CONTENT_TYPE_BASE64URL, contentTypeBase64Url);

                    SetResult(Result.Ok, resultIntent);
                    Log.Info(TAG, $"Finishing with error: {statusCode} - {errorMessage}");
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Failed to set error result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }
    }
}
