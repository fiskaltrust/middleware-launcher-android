namespace fiskaltrust.AndroidLauncher.Services
{
    public interface IMiddlewareProvider
    {
        Guid CashboxId { get; }
        string AccessToken { get; }
        string CountryCode { get; }
        Api.PosSystem.Core.Interfaces.IMiddlewareClient MiddlewareClient { get; }

        Task StartAsync();
        Task StopAsync();
    }
}
