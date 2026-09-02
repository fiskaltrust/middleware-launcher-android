using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    public class MiddlewareTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _package;
        private readonly string _version;
        private Guid _cashboxid;

        public MiddlewareTelemetryInitializer(string package, string version, Guid cashboxid)
        {
            _package = package;
            _version = version;
            _cashboxid = cashboxid;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is TraceTelemetry traceTelemetry)
            {
                AddDefaultProperties(traceTelemetry);
            }
            if (telemetry is RequestTelemetry requestTelemetry)
            {
                AddDefaultProperties(requestTelemetry);
            }
            if (telemetry is DependencyTelemetry dependencyTelemetry)
            {
                AddDefaultProperties(dependencyTelemetry);
            }
            else if (telemetry is ExceptionTelemetry exceptionTelemetry)
            {
                AddDefaultProperties(exceptionTelemetry);

                AddProperty(exceptionTelemetry, "StackTrace", Environment.StackTrace);
                AddProperty(exceptionTelemetry, "Device", DeviceInfo.Model);
                AddProperty(exceptionTelemetry, "DeviceManufacturer", DeviceInfo.Manufacturer);
                AddProperty(exceptionTelemetry, "DeviceType", DeviceInfo.Idiom);
                AddProperty(exceptionTelemetry, "AndroidVersion", DeviceInfo.VersionString);
            }
        }

        private void AddDefaultProperties(ISupportProperties traceTelemetry)
        {
            AddProperty(traceTelemetry, "Package", _package);
            AddProperty(traceTelemetry, "PackageVersion", _version);
            AddProperty(traceTelemetry, "CashboxId", _cashboxid);
        }

        private void AddProperty(ISupportProperties telemetry, string name, object value)
        {
            if (!telemetry.Properties.ContainsKey(name))
            {
                telemetry.Properties[name] = value.ToString();
            }
        }
    }
}