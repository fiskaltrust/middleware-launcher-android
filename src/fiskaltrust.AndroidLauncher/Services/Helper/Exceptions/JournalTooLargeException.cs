using fiskaltrust.ifPOS.v2.Cases;
using System;

namespace fiskaltrust.AndroidLauncher.Services.Helper.Exceptions
{
    /// <summary>
    /// Thrown by the Helipad uploader when a journal export grows beyond the configured upload limit before it is
    /// fully read. The export is aborted so the payload never has to be held in memory in full.
    /// </summary>
    public class JournalTooLargeException : Exception
    {
        public JournalTooLargeException(JournalType journalType, long bytes, long maxBytes)
            : base($"The journal 0x{journalType:X} exceeds the upload limit ({bytes} > {maxBytes} bytes).")
        {
        }
    }
}
