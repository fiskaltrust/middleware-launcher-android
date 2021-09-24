using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
{
  public static class ServiceCollectionExtensions
  {
    private static readonly Serilog.Core.Logger Logger = new LoggerConfiguration()
      .WriteTo.File(path: Path.Combine(FileLoggerHelper.LogDirectory.FullName, FileLoggerHelper.LogFilename), rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 5)
      .CreateLogger();
    
    public static IServiceCollection AddLogProviders(this IServiceCollection serviceCollection, LogLevel logLevel)
    {
      return serviceCollection.AddLogging(builder =>
      {
        builder.AddSerilog(Logger); 
        builder.Services.AddSingleton<ILoggerProvider, AndroidLoggerProvider>();
        builder.SetMinimumLevel(logLevel);
      });
    }
  }
}