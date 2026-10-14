using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Soap;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Android;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.ifPOS.v2;
using System.Text.Json;
using System.Net.Http.Json;

namespace fiskaltrust.AndroidLauncher.Signing
{

    /// <summary>
    /// IPLSSCD client for the PL signing service. The configured URL is the base of the v2 binding
    /// (see <see cref="Constants.Urls.SIGNING_PL_SANDBOX"/> and <see cref="Constants.Urls.SIGNING_PL_PRODUCTION"/>);
    /// the sandbox serves a fake register.
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
            var response = await _httpClient.PostAsJsonAsync("Echo", request);
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
            var response = await _httpClient.GetAsync("GetInfo");
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
            var response = await _httpClient.PostAsJsonAsync("ProcessReceipt", request);
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
        private const string PosNetPackage = "fiskaltrust.Middleware.SCU.PL.PosNet";

        /// <summary>
        /// Builds the SCU when it is hosted in this app. The returned provider owns the SCU and its
        /// connection to the device (over USB: the claimed interface), so the caller disposes it on stop;
        /// null means the SCU is not hosted here.
        /// </summary>
        public static ServiceProvider? CreateHostedScu(ILoggerFactory loggerFactory, PackageConfiguration? scuConfiguration)
        {
            // Single decision point: a bootstrapper means "host it here", null means "not ours".
            IMiddlewareBootstrapper? bootstrapper = scuConfiguration?.Package switch
            {
                PosNetPackage => new fiskaltrust.Middleware.SCU.PL.PosNet.ScuBootstrapper(),

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
            // Lets a usbhost:// DeviceUrl open the printer through the Android USB host API.
            serviceCollection.AddPosNetAndroidUsbHost(Android.App.Application.Context);

            bootstrapper.Id = scuConfiguration!.Id;
            bootstrapper.Configuration = scuConfiguration.Configuration;
            bootstrapper.ConfigureServices(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Asks the user for access to a PosNet printer attached over USB, which Android requires before
        /// the SCU can open it. Without it the SCU still starts, but reports the printer as unreachable.
        /// </summary>
        public static async Task RequestUsbPermissionAsync(PackageConfiguration? scuConfiguration)
        {
            if (scuConfiguration?.Package != PosNetPackage
                || PosNetConfiguration.FromConfiguration(scuConfiguration.Configuration).ParseDeviceAddress() is not PosNetDeviceAddress.UsbHost usb)
            {
                return;
            }

            if (await PosNetUsbPermission.RequestAsync(Android.App.Application.Context, usb))
            {
                Serilog.Log.Logger.Information("Access to the USB device {device} is granted.", usb);
            }
            else
            {
                Serilog.Log.Logger.Warning("No access to the USB device {device}: it is not attached, or access was denied.", usb);
            }
        }

        private static bool IsHostedByMe(string? package)
            => package is not null && package.StartsWith(PolishScuPackagePrefix, StringComparison.InvariantCulture);
    }
}
