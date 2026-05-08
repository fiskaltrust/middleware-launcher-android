using Android.Content;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.Api.PosSystem.Core.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Extensions
{
    public static class PosSystemApiRequestCore
    {
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

            if (!headers.ContainsKey("x-operation-id"))
                throw new ArgumentException("The required header x-operation-id was not sent.");

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

            return new PosSystemApiRequest
            {
                Method = method,
                Path = path,
                Headers = headers,
                Body = body
            };
        }
    }
    public static class PosSystemApiResponseCoreExtensions
    {
        /// <summary>
        /// Encodes the response data for use in an Android Intent
        /// </summary>
        /// <param name="response">The response to encode</param>
        /// <returns>An object containing Base64URL-encoded response data</returns>
        public static IntentResponseData ToIntentData(this Api.PosSystem.Core.Models.PosSystemApiResponse response)
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

    }
}
