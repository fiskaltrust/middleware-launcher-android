using Microsoft.Extensions.Primitives;

namespace fiskaltrust.Api.POS.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<string, string[]> Filter(this Dictionary<string, StringValues> dictionary, Func<KeyValuePair<string, StringValues>, bool> predicate)
    {
        return dictionary.Where(predicate).Select(x => (x.Key, x.Value.ToArray())).ToDictionary();
    }

    public static Dictionary<string, string[]> Filter(this Dictionary<string, string> dictionary, Func<KeyValuePair<string, string>, bool> predicate)
    {
        return dictionary.Where(predicate).Select(x => (x.Key, new[] { x.Value })).ToDictionary();
    }
}
