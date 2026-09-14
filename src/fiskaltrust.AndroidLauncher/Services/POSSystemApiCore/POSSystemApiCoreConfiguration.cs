using fiskaltrust.Api.PosSystem.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace fiskaltrust.AndroidLauncher.Services.POSSystemApiCore;

public class POSSystemApiCoreConfiguration
{
    [Required]
    public Guid CashBoxId { get; set; }
    [Required(AllowEmptyStrings = false)]
    public string AccessToken { get; set; } = null!;
    [Required(AllowEmptyStrings = false)]
    public string Configuration { get; set; } = null!;
    [Required(AllowEmptyStrings = false)]
    public string MessageBusUri { get => AppEnvironment == AppEnvironments.Production ? "gateway.fiskaltrust.eu/mqtt" : "gateway-sandbox.fiskaltrust.eu/mqtt"; set; }
    [Required(AllowEmptyStrings = false)]
    public AppEnvironments AppEnvironment { get; set; }
    [Required(AllowEmptyStrings = false)]
    public LauncherEnvironments LauncherEnvironment { get; set; } = LauncherEnvironments.Local;

}
