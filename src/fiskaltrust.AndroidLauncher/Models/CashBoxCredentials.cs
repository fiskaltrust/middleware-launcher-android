using System;

namespace fiskaltrust.AndroidLauncher.Models
{
    public class CashBoxCredentials
    {
        public required Guid CashBoxId { get; set; }
        public required string AccessToken { get; set; }
        public required Guid QueueId { get; set; }
    }
}
