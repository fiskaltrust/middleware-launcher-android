using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Soap;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.ifPOS.v2;
using System.Text.Json;
using System.Net.Http.Json;

namespace fiskaltrust.AndroidLauncher.Signing
{

    /// <summary>
    /// IPLSSCD client for the PL signing service. In the sandbox this talks to the fake register
    /// hosted at signing-sandbox.fiskaltrust.pl/sandbox; the production register SCU (Posnet)
    /// follows the same v2 binding once it is available.
    /// </summary>
    public class SigningIPLSSCD : IPLSSCD
    {
        private readonly HttpClient _httpClient;

        public SigningIPLSSCD(Guid cashBoxId, string accessToken, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.Url.EndsWith("/"))
                clientConfiguration.Url = clientConfiguration.Url + "/";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(clientConfiguration.Url)
            };
            _httpClient.DefaultRequestHeaders.Add("x-cashbox-id", cashBoxId.ToString());
            _httpClient.DefaultRequestHeaders.Add("x-cashbox-accesstoken", accessToken.ToString());
        }

        public async Task<EchoResponse> EchoAsync(EchoRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/sandbox/v2/Echo", request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to echo with result " + await response.Content.ReadAsStringAsync() + ". Status code: " + response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<EchoResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<PLSSCDInfo> GetInfoAsync()
        {
            var response = await _httpClient.GetAsync("/sandbox/v2/GetInfo");
            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("Failed to get PLSSCD info with result " + await response.Content.ReadAsStringAsync() + ". Status code: " + response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PLSSCDInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/sandbox/v2/ProcessReceipt", request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to process receipt with result " + await response.Content.ReadAsStringAsync() + ". Status code: " + response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProcessResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    public static class PLSSCDFactory
    {
        private const string PolishScuPackagePrefix = "fiskaltrust.Middleware.SCU.PL.";

        public static IPLSSCD CreateHostedScu(ILoggerFactory loggerFactory, PackageConfiguration? scuConfiguration)
        {
                // Single decision point: a bootstrapper means "host it here", null means "not ours".
                IMiddlewareBootstrapper? bootstrapper = scuConfiguration?.Package switch
                {
                    "fiskaltrust.Middleware.SCU.PL.PosNet" => new fiskaltrust.Middleware.SCU.PL.PosNet.ScuBootstrapper(),

                    // No device declared, or a device of another market — not ours.
                    _ when !IsHostedByMe(scuConfiguration?.Package) => null,

                    _ => throw new NotSupportedException($"The selected SCU is not supported by this platform ({scuConfiguration!.Package}).")
                };

                if (bootstrapper is null)
                {
                    return null;
                }

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton(loggerFactory);
                serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

                bootstrapper.Id = scuConfiguration!.Id;
                bootstrapper.Configuration = scuConfiguration.Configuration;
                bootstrapper.ConfigureServices(serviceCollection);
    // test
                return serviceCollection.BuildServiceProvider().GetRequiredService<IPLSSCD>();
            }

            private static bool IsHostedByMe(string? package)
                => package is not null && package.StartsWith(PolishScuPackagePrefix, StringComparison.InvariantCulture);
    }
}
