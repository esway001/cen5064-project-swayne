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

            var p = new Player(connId, name, number, (number - 1) * 2f, 0f, 0f, 0);
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

    //IF MAKE CHANGES ON STEP, MATCH IN SHARED\MOVEMENT.JS
    public void Step(float dt)
    {
        foreach (var (id, p) in _players)
        {
            //get latest input from player
            var input = _inputs.GetValueOrDefault(id);
            uint seq = input?.Seq ?? p.LastSeq; //save input seq
            float ix = input?.X ?? 0f;
            float iz = input?.Z ?? 0f;

            // fix diagonals to standardize speed
            float len = MathF.Sqrt(ix * ix + iz * iz);
            if(len >0f) { ix /= len; iz /= len; }

            //get rid of old position write and replace with per axis
            float vx = ix * Speed * dt;
            float vz = iz * Speed * dt;

            //collide and slide movement (check X first then Z, matches movement.js
            float x = p.X, z = p.Z;

            float nx = x + vx;
            if (!HitsAny(nx, z)) x = nx; //x move only if no wall

            float nz = z + vz;
            if (!HitsAny(nz, x)) z = nz; //z move only if no wall, x may have been updated at this point

            //here is the new state return

            _players[id] = p with { X = x, Z = z, LastSeq = seq };
        }
    }

    private static bool HitsAny(float x, float z)
    {
        foreach (var w in Level.Walls)
            if (HitsWall(x, z, w)) return true;
        return false;
    }

    private static bool HitsWall(float x, float z, Wall w)
    {
        // check for the closest point on the box to x,z
        float cx = MathF.Max(w.MinX, MathF.Min(x, w.MaxX));
        float cz = MathF.Max(w.MinZ, MathF.Min(z, w.MaxZ));
        float dx = x - cx, dz = z - cz;
        return dx * dx + dz * dz < Level.PlayerRadius * Level.PlayerRadius;
    }
}

