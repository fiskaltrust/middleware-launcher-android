using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddLogProviders(this IServiceCollection serviceCollection, LogLevel logLevel)
    {
      return serviceCollection.AddLogging(builder =>
      {
        builder.AddSerilog(); 
        builder.Services.AddSingleton<ILoggerProvider, AndroidLoggerProvider>();
        builder.SetMinimumLevel(logLevel);
      });
    }
  }
}