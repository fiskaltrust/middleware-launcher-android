using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common.Models
{
    public class LauncherParameters
    {
        public Guid CashboxId { get; set; }
        public string AccessToken { get; set; }
        public bool IsSandbox { get; set; }
        public bool EnableCloseButton { get; set; }
        public LogLevel LogLevel { get; set; }
        public Dictionary<string, object> ScuParams { get; set; }
    }
}