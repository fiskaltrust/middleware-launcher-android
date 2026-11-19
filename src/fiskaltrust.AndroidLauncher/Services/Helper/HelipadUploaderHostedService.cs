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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Helipad Uploader Hosted Service is stopping.");
            try
            {
                await _helipadUploader.Stop(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Helipad upload still running after the shutdown timeout; stopping without waiting for it.");
            }
        }
    }
}
