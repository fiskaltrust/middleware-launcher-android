using System;
using System.Text;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    /// <summary>
    /// Helper class for Base64URL encoding/decoding as specified in RFC 4648 §5
    /// </summary>
    public static class Base64UrlHelper
    {
        /// <summary>
        /// Encodes a string to Base64URL format (no padding, replace + with -, / with _)
        /// </summary>
        public static string Encode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            return EncodeBytes(bytes);
        }

        /// <summary>
        /// Encodes bytes to Base64URL format (no padding, replace + with -, / with _)
        /// </summary>
        public static string EncodeBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            var base64 = Convert.ToBase64String(bytes);
            return base64
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// Decodes a Base64URL string to a regular string
        /// </summary>
        public static string Decode(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url))
            {
                return string.Empty;
            }

            var bytes = DecodeBytes(base64Url);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Decodes a Base64URL string to bytes
        /// </summary>
        public static byte[] DecodeBytes(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url))
            {
                return Array.Empty<byte>();
            }

            // Convert Base64URL to standard Base64
            var base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
