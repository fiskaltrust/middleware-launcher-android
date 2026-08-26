using System.Text;
using System.Text.Json;
using Android.Content;
using Android.OS;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    /// <summary>
    /// A PosSystemAPI request as passed over the Intent / Messenger surface, with defaults
    /// for every value so the test client can also be launched without arguments.
    /// </summary>
    internal sealed class PosSystemApiRequest
    {
        public const string KeyMethod = "Method";
        public const string KeyPath = "Path";
        public const string KeyHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
        public const string KeyBodyBase64Url = "BodyBase64Url";
        public const string KeyStatusCode = "StatusCode";

        public string Method { get; }
        public string Path { get; }
        public string HeaderJsonBase64Url { get; }
        public string BodyBase64Url { get; }

        private PosSystemApiRequest(string method, string path, string headerJsonBase64Url, string bodyBase64Url)
        {
            Method = method;
            Path = path;
            HeaderJsonBase64Url = headerJsonBase64Url;
            BodyBase64Url = bodyBase64Url;
        }

        public static PosSystemApiRequest FromExtras(Func<string, string?> getExtra) => new(
            getExtra(KeyMethod) ?? "POST",
            getExtra(KeyPath) ?? "/v2/echo",
            getExtra(KeyHeaderJsonBase64Url) ?? DefaultHeadersBase64Url(),
            getExtra(KeyBodyBase64Url) ?? ToBase64Url(JsonSerializer.Serialize(new { Message = "Ping" })));

        public static PosSystemApiRequest Create(string method, string path, IReadOnlyDictionary<string, string> headers, string body) => new(
            method,
            path,
            ToBase64Url(JsonSerializer.Serialize(headers)),
            ToBase64Url(body));

        public void WriteTo(Intent intent)
        {
            intent.PutExtra(KeyMethod, Method);
            intent.PutExtra(KeyPath, Path);
            intent.PutExtra(KeyHeaderJsonBase64Url, HeaderJsonBase64Url);
            intent.PutExtra(KeyBodyBase64Url, BodyBase64Url);
        }

        public void WriteTo(Bundle bundle)
        {
            bundle.PutString(KeyMethod, Method);
            bundle.PutString(KeyPath, Path);
            bundle.PutString(KeyHeaderJsonBase64Url, HeaderJsonBase64Url);
            bundle.PutString(KeyBodyBase64Url, BodyBase64Url);
        }

        public override string ToString() => $"{Method} {Path}";

        private static string DefaultHeadersBase64Url()
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };
            return ToBase64Url(JsonSerializer.Serialize(headers));
        }

        private static string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
