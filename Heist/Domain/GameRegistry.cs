using System.Collections.Concurrent;

namespace Heist.Domain;


//World State, in memory, won't touch supabase. Add players, remove, get all players, singleton service for DI
public class GameRegistry
{
    private const int MaxPlayers = 4;
    private readonly ConcurrentDictionary<string, Player> _players = new();
    private readonly object _gate = new();

    public Player? Add(string connId, string name)
    {
        lock (_gate)            //this prevents players from taking the same slot, each player gets one slot 1-4
        {
            var taken = _players.Values.Select(p => p.Number).ToHashSet();
            int number = Enumerable.Range(1, MaxPlayers).FirstOrDefault(n => !taken.Contains(n));
            if (number == 0) return null;                   //too many players, reject attempt

            var p = new Player(connId, name, number, (number - 1) * 2f, 0f, 0f);
            _players[connId] = p;
            return p;
        }
    }

    public void Remove(string connId) => _players.TryRemove(connId, out _);
    public IReadOnlyCollection<Player> All => _players.Values.ToArray();
}

