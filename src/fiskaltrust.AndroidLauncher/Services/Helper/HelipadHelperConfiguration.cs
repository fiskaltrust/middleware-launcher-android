using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public class HelipadHelperConfiguration
    {
        // Records per journal read. The uploader materialises every batch in memory several
        // times over (list of chunks, ToArray copy, MemoryStream), so 10,000 large GR/IT queue
        // items per read pushed the EU pods to 4-8 GB and OOM'd their nodes (2026-09-04..06).
        // Lower defaults cost a few more round-trips on a big backlog and nothing else.
        // Both can still be raised per cashbox via the Helipad helper configuration
        // ("maxrecordcount" / "startrecordcount").
        public int MaxReadCount { get; set; } = 1000;

        public int StartRecordCount { get; set; } = 200;

        public int MaxJournalChunkSize { get; set; } = 1024 * 1024; // 1 MB

        public const long DefaultMaxUploadBytes = 16 * 1024 * 1024; // 16 MB

        /// <summary>
        /// Hard ceiling for any upload payload, whatever the configuration or the record says: the configured
        /// <see cref="MaxUploadBytes"/> is clamped to it, and a single record (which cannot be split and is therefore
        /// not subject to the configured limit) is aborted beyond it. It exists so that neither a typo in the helper
        /// configuration nor a runaway export can pull an unbounded amount of data into memory again.
        /// </summary>
        public const long MaxUploadBytesCeiling = 256L * 1024 * 1024; // 256 MB

        /// <summary>
        /// Upper bound for a single upload payload. Journal exports that grow beyond this are aborted and retried
        /// with fewer records, which keeps the uploader's memory footprint bounded per cashbox.
        /// </summary>
        public long MaxUploadBytes { get; set; } = DefaultMaxUploadBytes;

        public static HelipadHelperConfiguration FromDictionary(Dictionary<string, object>? configuration)
        {
            var helipadHelperConfiguration = new HelipadHelperConfiguration();

            if (configuration == null)
            {
                return helipadHelperConfiguration;
            }

            if (configuration.ContainsKey("maxuploadbytes"))
            {
                if (long.TryParse(configuration["maxuploadbytes"].ToString(), out var maxUploadBytes) && maxUploadBytes > 0)
                {
                    helipadHelperConfiguration.MaxUploadBytes = Math.Min(maxUploadBytes, MaxUploadBytesCeiling);
                }
            }

            if (configuration.ContainsKey("maxjournalchunksize"))
            {
                if (int.TryParse(configuration["maxjournalchunksize"].ToString(), out var maxJournalChunkSize) && maxJournalChunkSize > 0)
                {
                    helipadHelperConfiguration.MaxJournalChunkSize = maxJournalChunkSize;
                }
            }

            if (configuration.ContainsKey("maxrecordcount"))
            {
                if (int.TryParse(configuration["maxrecordcount"].ToString(), out var maxReadCount) && maxReadCount > 0)
                {
                    helipadHelperConfiguration.MaxReadCount = maxReadCount;
                }
            }

            if (configuration.ContainsKey("startrecordcount"))
            {
                if (int.TryParse(configuration["startrecordcount"].ToString(), out var startReadCount) && startReadCount > 0)
                {
                    helipadHelperConfiguration.StartRecordCount = startReadCount;
                }
            }

            return helipadHelperConfiguration;
        }
    }
}
