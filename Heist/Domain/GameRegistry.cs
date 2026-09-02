using System.Collections.Concurrent;

namespace Heist.Domain;


//World State, in memory, won't touch supabase. Add players, remove, get all players, singleton service for DI
public class GameRegistry
{
    private readonly ConcurrentDictionary<string, Player> _players = new();
    public Player Add(string connId, string name)
    {
        var i = _players.Count; //jyust count players for tests
        var p = new Player(connId, name, i * 2f, 0f, 0f);
        _players[connId] = p;
        return p;
    }

    public void Remove(string connId) => _players.TryRemove(connId, out _);
    public IReadOnlyCollection<Player> All => _players.Values.ToArray();
}

