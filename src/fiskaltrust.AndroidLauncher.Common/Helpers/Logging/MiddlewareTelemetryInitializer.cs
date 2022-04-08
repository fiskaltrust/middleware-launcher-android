using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
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
            if(telemetry is DependencyTelemetry dependencyTelemetry)
            {
                AddDefaultProperties(dependencyTelemetry);
            }
            else if (telemetry is ExceptionTelemetry exceptionTelemetry)
            {
                AddDefaultProperties(exceptionTelemetry);

                AddProperty(exceptionTelemetry, "CurrentDirectory", Environment.CurrentDirectory);
                AddProperty(exceptionTelemetry, "StackTrace", Environment.StackTrace);
                AddProperty(exceptionTelemetry, "Is64BitProcess", Environment.Is64BitProcess);
                AddProperty(exceptionTelemetry, "CLRVersion", Environment.Version);
                AddProperty(exceptionTelemetry, "OSVersion", Environment.OSVersion);
                AddProperty(exceptionTelemetry, "Is64BitOperatingSystem", Environment.Is64BitOperatingSystem);
                AddProperty(exceptionTelemetry, "SystemDirectory", Environment.SystemDirectory);
                AddProperty(exceptionTelemetry, "SystemPageSize", Environment.SystemPageSize);
                AddProperty(exceptionTelemetry, "UserInteractive", Environment.UserInteractive);
                AddProperty(exceptionTelemetry, "HasShutdownStarted", Environment.HasShutdownStarted);
                AddProperty(exceptionTelemetry, "ProcessorCount", Environment.ProcessorCount);
                AddProperty(exceptionTelemetry, "WorkingSet", Environment.WorkingSet);
            }

            telemetry.Context.Operation.Id = CorrelationManager.GetOperationId();
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