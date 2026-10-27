using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Heist.Domain;

public class LevelLoader
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<LevelLoader> _log;

    public LevelLoader(ILogger<LevelLoader> log)
    {
        _log = log;
    }

    public Dictionary<string, LevelFile> Load(string levelsDir)
    {
        var levels = new Dictionary<string, LevelFile>();

        foreach (var path in Directory.EnumerateFiles(levelsDir, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(path);

            try 
            {
                var level = JsonSerializer.Deserialize<LevelFile>(
                    File.ReadAllText(path),Opts)
                    ?? throw new Exception("Deserialized level is null");

                var error = Validate(level);

                if (error is not null)
                {
                    _log.LogError(
                        "Level '{Id}' rejected: {Error}", id, error);
                    continue;
                }

                levels[id] = level;

                _log.LogInformation(
                    "Level '{Id}' loaded", id);
            }
            catch(Exception ex)
            {
                _log.LogError(
                    ex, "Level '{Id}' failed to load", id);
            }
        }
        return levels;
    }

    private static string? Validate(LevelFile l)
    {
        if (l.FormatVersion != 1)
            return $"formatVersion {l.FormatVersion} != 1";

        if (l.Walls is null ||
            l.Hazards is null ||
            l.Spawns is null ||
            l.Objective is null)
            return "missing walls, hazards, spawns or objective";

        if (l.Spawns.Count != 4)
            return $"{l.Spawns.Count} spawns, 4 required";

        foreach(var w in l.Walls)
        {
            if (w.MinX >= w.MaxX || w.MinZ >= w.MaxZ)
                return $"wall '{w.name}' inverted wal bounds";
        }

        foreach(var h in l.Hazards)
        {
            if(h.MinX >= h.MaxX || h.MinZ >= h.MaxZ)
                return $"hazard '{h.name}' inverted hazard bounds";

            if (h.SafeMs < 100 || h.ArmedMs < 100)
                return $"hazard '{h.name}' timing < 100ms";
        }
        return null;
    }
}
