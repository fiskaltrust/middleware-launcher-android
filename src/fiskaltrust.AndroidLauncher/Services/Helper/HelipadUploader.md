# Helipad Uploader

`HelipadUploader` periodically exports the configuration journal and then uploads the action, receipt, and queue-item journals. The timer waits 30 seconds before its first run and schedules the next run after the current upload finishes.

## Record-count settings

The Helipad helper configuration supports these settings:

| Setting | Default | Behavior |
| --- | ---: | --- |
| `startrecordcount` | 200 | Initial number of records requested per journal read. |
| `maxrecordcount` | 1000 | Upper bound used when adapting the count. |
| `maxuploadbytes` | 16 MiB | Maximum payload size for a journal read when requesting more than one record. Values are capped at 256 MiB. |
| `maxjournalchunksize` | 1 MiB | Parsed by the helper configuration; it does not control the uploader's record-count adaptation. |

Only positive integer values are accepted. Missing or invalid values leave the defaults in effect.

## How the count changes

The current count is stored in the `HelipadUploader` instance. It is reused between that instance's upload cycles, but is not persisted to disk; a new uploader instance starts from configuration again.

The action, receipt, and queue-item journals share one count. Each starts with the current value, but if an earlier journal reduces it during the cycle, later journals use the reduced value. Configuration is exported separately as a complete journal (`From = 0`, `To = 0`) and does not use or contribute payload bytes to record-count adaptation.

After the journal reads:

- **Oversized payload:** If the local byte limit is exceeded or Helipad returns HTTP 413, the count is reduced to one quarter, with a minimum of 1. The next upload cycle retries from the same journal pointer; the current cycle does not retry immediately.
- **One record is still too large:** The count is pinned at 1 and an error is logged. A one-record request uses the 256 MiB hard ceiling instead of the configured `maxuploadbytes`, because a record cannot be split.
- **Other journal upload error:** The count is reduced to `current / 2 + 1` (integer division). That entity's pointer does not advance, so a later cycle can retry it with the lower count.
- **Configuration export error:** The count is also reduced to `current / 2 + 1`; journal uploads continue afterward.
- **Successful non-empty uploads with no count reduction during the cycle:** The uploader tries to make the largest journal payload about half of `maxuploadbytes` next time. If the payload was smaller than that target, it raises the record count; if it was larger, it lowers the count. For example, with a current count of 200 and a 16 MiB limit, the target is 8 MiB. A largest payload of 4 MiB doubles the next count to 400; a 16 MiB payload halves it to 100. The result is limited to the range 1 through `maxrecordcount`. Empty exports do not increase the count. If any journal upload adjusted the count during the cycle, this proportional adjustment is skipped.

The implementation for successful uploads and oversized payloads is:

```csharp
internal static int NextRecordCount(int recordCount, long largestPayload, long maxUploadBytes, int maxReadCount)
{
	var target = maxUploadBytes / 2;
	var next = (long)recordCount * target / Math.Max(1, largestPayload);
	return (int)Math.Clamp(next, 1, Math.Max(1, maxReadCount));
}

internal static int ShrinkRecordCountForOversizedPayload(int recordCount)
	=> Math.Max(1, recordCount / 4);
```

## Configuration considerations

Keep `startrecordcount` at or below `maxrecordcount`. The current implementation seeds the initial count directly from `startrecordcount`; it does not clamp that initial seed before the first journal read. The successful-upload adaptation clamps subsequent values to `maxrecordcount`.

The uploader shares one adaptive count across all three journal types. A large payload from any one type can lower the count used for all three on the next cycle.