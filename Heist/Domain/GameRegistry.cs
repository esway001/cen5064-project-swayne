using System.Collections.Concurrent;

namespace Heist.Domain;


//World State, in memory, won't touch supabase. Add players, remove, get all players, singleton service for DI
public class GameRegistry
{
    private const int MaxPlayers = 4;
    private const float Speed = 5f;                         //world units per second
    private readonly ConcurrentDictionary<string, Player> _players = new();
    private readonly ConcurrentDictionary<string, InputCommand> _inputs = new();
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

    public void Remove(string connId){
        _players.TryRemove(connId, out _);
        _inputs.TryRemove(connId, out _); //remove stale input
    }
    public IReadOnlyCollection<Player> All => _players.Values.ToArray();

    public void SetInput(string connId, InputCommand cmd) => _inputs[connId] = cmd;

    public void Step(float dt)
    {
        foreach (var (id, p) in _players)
        {
            var input = _inputs.GetValueOrDefault(id);
            float ix = input?.X ?? 0f;
            float iz = input?.Z ?? 0f;

            // fix diagonals to standardize speed
            float len = MathF.Sqrt(ix * ix + iz * iz);
            if(len >0f) { ix /= len; iz /= len; }

            float newX = p.X + ix * Speed * dt;
            float newZ = p.Z + iz * Speed * dt;

            _players[id] = p with { X = newX, Z = newZ };
        }
    }
}

