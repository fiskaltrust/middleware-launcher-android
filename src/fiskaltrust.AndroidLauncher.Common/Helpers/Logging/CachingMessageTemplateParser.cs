using System;
using System.Collections;
using Serilog.Events;
using Serilog.Parsing;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
  class CachingMessageTemplateParser
  {
    readonly MessageTemplateParser _innerParser = new MessageTemplateParser();
        
    readonly object _templatesLock = new object();
    readonly Hashtable _templates = new Hashtable();

    const int MaxCacheItems = 1000;
    const int MaxCachedTemplateLength = 1024;

    public MessageTemplate Parse(string messageTemplate)
    {
      if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

      if (messageTemplate.Length > MaxCachedTemplateLength)
        return _innerParser.Parse(messageTemplate);

      // ReSharper disable once InconsistentlySynchronizedField
      // ignored warning because this is by design
      var result = (MessageTemplate)_templates[messageTemplate];
      if (result != null)
        return result;

      result = _innerParser.Parse(messageTemplate);

      lock (_templatesLock)
      {
        // Exceeding MaxCacheItems is *not* the sunny day scenario; all we're doing here is preventing out-of-memory
        // conditions when the library is used incorrectly. Correct use (templates, rather than
        // direct message strings) should barely, if ever, overflow this cache.

        // Changing workloads through the lifecycle of an app instance mean we can gain some ground by
        // potentially dropping templates generated only in startup, or only during specific infrequent
        // activities.

        if (_templates.Count == MaxCacheItems)
          _templates.Clear();

        _templates[messageTemplate] = result;
      }

      return result;
    }
  }
}