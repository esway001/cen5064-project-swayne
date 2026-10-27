using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Heist.Domain;

public class LevelCatalog
{
    private readonly Dictionary<string, LevelFile> _levels; //dictionary of level files json

    public LevelCatalog(Dictionary<string, LevelFile> levels)
    {
        if (levels.Count == 0) throw new InvalidOperationException($"No valid levels - nothing is playable.");
        _levels = levels;
    }

    public LevelFile Get(string id) => _levels[id];
    public bool Has(string id) => _levels.ContainsKey(id);
    public IReadOnlyCollection<string> Ids => _levels.Keys;

}
