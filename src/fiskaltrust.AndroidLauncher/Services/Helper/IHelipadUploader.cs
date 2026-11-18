namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public interface IHelipadUploader
    {
        void Start();
        Task Stop(CancellationToken cancellationToken = default);
        Task ProcessUpload();
    }
}
