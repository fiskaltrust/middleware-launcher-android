using System;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.AndroidLauncher.Services.Helper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.CloudCashbox.Services
{
    public class HelipadUploaderHostedService : IHostedService
    {
        private readonly ILogger<HelipadUploaderHostedService> _logger;
        private readonly IHelipadUploader _helipadUploader;

        public HelipadUploaderHostedService(ILogger<HelipadUploaderHostedService> logger, IHelipadUploader helipadUploader)
        {
            _logger = logger;
            _helipadUploader = helipadUploader;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _helipadUploader.Start();
            _logger.LogTrace("Helipad Uploader Hosted Service is running.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _helipadUploader.Stop();
            _logger.LogTrace("Helipad Uploader Hosted Service is stopping.");

            return Task.CompletedTask;
        }
    }
}
