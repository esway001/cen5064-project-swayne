using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Heist.Domain;

public class LevelCatalog
{
    private readonly Dictionary<string, LevelFile> _levels = new(); //dictionary of level files json
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public LevelCatalog(string levelsDir, ILogger<LevelCatalog> log)
    {
        foreach (var path in Directory.EnumerateFiles(levelsDir, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            try
            {
                var lvl = JsonSerializer.Deserialize<LevelFile>(File.ReadAllText(path), Opts)
                    ?? throw new Exception("deserialized to null");
                var error = Validate(lvl);
                if(error is not null) { log.LogError("Level '{Id}' rejected: {Error}", id, error); continue; }
                _levels[id] = lvl;
                log.LogInformation("Level '{Id}' loaded", id);
            }
            catch (Exception ex) { log.LogError(ex, "Level '{Id}' failed to load", id); }
        }
        if (_levels.Count == 0) throw new InvalidOperationException($"No valid levels in {levelsDir} - nothing is playable.");
    }

    public LevelFile Get(string id) => _levels[id];
    public bool Has(string id) => _levels.ContainsKey(id);
    public IReadOnlyCollection<string> Ids => _levels.Keys;

    private static string? Validate(LevelFile l)
    {
        if (l.FormatVersion != 1) return $"formatVersion {l.FormatVersion} != 1";
        if (l.Walls is null || l.Hazards is null || l.Spawns is null || l.Objective is null) return "missing walls, hazards, spawns or objective";
        if(l.Spawns.Count != 4) return $"{l.Spawns.Count} spawns, need 4";
        foreach (var w in l.Walls)
            if (w.MinX >= w.MaxX || w.MinZ >= w.MaxZ) return $"wall '{w.name}' inverted wall bounds";
        foreach(var h in l.Hazards)
        {
            if (h.MinX >= h.MaxX || h.MinZ >= h.MaxZ) return $"hazard '{h.name}' inverted hazard bounds";
            if (h.SafeMs < 100 || h.ArmedMs < 100) return $"hazard '{h.name}' timing < 100ms";
        }
        return null;
    }
}
