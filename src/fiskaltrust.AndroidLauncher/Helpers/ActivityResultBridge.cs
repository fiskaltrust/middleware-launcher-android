using Android.Content;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public static class ActivityResultBridge
    {
        public const int PickFolderRequestCode = 4242;

        public static TaskCompletionSource<Intent?>? PendingPickFolderResult { get; set; }
    }
}
