using fiskaltrust.AndroidLauncher.Models;
using fiskaltrust.storage.serialization.V0;
using System;

namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public interface IHelipadUploader
    {
        void Start();
        void Stop();
        Task ProcessUpload();
    }
}
