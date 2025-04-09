using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Common.Signing
{
    public class AbstractScuList
    {
        private readonly Dictionary<string, object> _scus = new();

        public void Add(string url, object scu) => _scus.Add(url, scu);

        public Dictionary<string, T> OfType<T>() => _scus.Where(x => x.Value is T).ToDictionary(x => x.Key, x => (T)x.Value);
    }
}