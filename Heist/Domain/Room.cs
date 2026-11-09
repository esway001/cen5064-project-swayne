using System.Collections.Concurrent;

namespace Heist.Domain;


//World State, in memory, won't touch supabase. Add players, remove, get all players, singleton service for DI
public class Room
{
    private readonly LevelFile _level;
    public string LevelId { get; }
    public string Code { get; }
    public string? HostConnId { get; set; }
    public int PlayerCount => _players.Count;

    public Room(string code, string levelId, LevelFile level)
    {
        Code = code;
        LevelId = levelId;
        _level = level;
        _trapArmed = new bool[level.Hazards.Count];
        _doors = level.Doors ?? Array.Empty<DoorData>();
        _triggers = level.Triggers ?? Array.Empty<TriggerData>();
        _doorOpen = new bool[_doors.Count]; //all doors start closed
        _doorTimer = new float[_doors.Count]; 

    }

    public RoomStatus Status { get; private set; } = RoomStatus.Waiting;

    public void StartGame()
    {
        if (Status != RoomStatus.Waiting) return;        // start game once
        Status = RoomStatus.InProgress;
        _elapsedMs = 0;                                  // traps start at game start
    }

    public bool IsEmpty => _players.IsEmpty;

    private const int MaxPlayers = 4;
    private const float Speed = 5f;                         //world units per second
    private readonly ConcurrentDictionary<string, Player> _players = new();
    private readonly ConcurrentDictionary<string, InputCommand> _inputs = new();
    private readonly object _gate = new();

    private double _elapsedMs;
    private readonly bool[] _trapArmed; //= new bool[Level.Hazards.Length]; // moved to ctor
    private readonly IReadOnlyList<DoorData> _doors;
    private readonly IReadOnlyList<TriggerData> _triggers;
    private readonly bool[] _doorOpen;
    private readonly float[] _doorTimer;

    public IReadOnlyList<bool> TrapStates => _trapArmed;            //reported by snapshot
    public IReadOnlyList<DoorData> Doors => _doors;
    public IReadOnlyList<bool> DoorOpen => _doorOpen;

    public GameState State { get; private set; } = GameState.Playing;

    public Player? Add(string connId, string name)
    {
        lock (_gate)            //this prevents players from taking the same slot, each player gets one slot 1-4
        {
            var taken = _players.Values.Select(p => p.Number).ToHashSet();
            int number = Enumerable.Range(1, MaxPlayers).FirstOrDefault(n => !taken.Contains(n));
            if (number == 0) return null;                   //too many players, reject attempt

            var s = _level.Spawns[number - 1];
            var p = new Player(connId, name, number, s.X, 0f, s.Z, 0);
            _players[connId] = p;
            return p;
        }
    }

    public void Remove(string connId){
        _players.TryRemove(connId, out _);
        _inputs.TryRemove(connId, out _); //remove stale input
    }
    public IReadOnlyCollection<Player> All => _players.Values.ToArray();

    public void SetInput(string connId, InputCommand cmd) 
    {
        //AI CODE
        if(State != GameState.Playing) return;
        // END OF AI CODE
        _inputs[connId] = cmd; 
    }

    //IF MAKE CHANGES ON STEP, MATCH IN SHARED\MOVEMENT.JS
    public void Step(float dt)
    {
        //AI CODE 
        if(State != GameState.Playing)
        {
            _inputs.Clear();
            return;
        }
        //END OF AI CODE

        _elapsedMs += dt * 1000.0;


        //Clarifying comment: loop arms hazards
        for (int i = 0; i < _level.Hazards.Count; i++)
        {
            var h = _level.Hazards[i];
            var cycle = h.SafeMs + h.ArmedMs;
            var t = _elapsedMs % cycle;
            _trapArmed[i] = t >= h.SafeMs;          //safe then armed
        }

        //Triggers and doors
        for (int d = 0; d < _doors.Count; d++)
        {
            var door = _doors[d];
            bool hasController = false;
            bool allSatisfied = true;

            foreach (var t in _triggers)
            {
                if (!t.Targets.Contains(door.name)) continue; //skip triggers that don't control this door
                hasController = true;
                if(!TriggerSatisfied(t)) { allSatisfied = false; break; }
            }

            bool conditionMet = hasController && allSatisfied; //if no controller, door stays closed, if has controller, all must be satisfied

            if (conditionMet)
            {
                _doorOpen[d] = true;
                _doorTimer[d] = door.CloseDelayMs;//hold open, grace timer reset
            }
            else if (_doorOpen[d])
            {
                _doorTimer[d] -= dt * 1000f;
                if (_doorTimer[d] <= 0f) _doorOpen[d] = false;
            }
        }

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
            if (!HitsAny(x, nz)) z = nz; //z move only if no wall, x may have been updated at this point

            //here is the new state return

            _players[id] = p with { X = x, Z = z, LastSeq = seq };
        }

        //for each player, if they step inside the hazard, end the game as a loss
        foreach (var p in _players.Values)
        {
            for (int i =0; i< _level.Hazards.Count; i++)
            {
                if (!_trapArmed[i]) continue;
                var h = _level.Hazards[i];
                float cx = MathF.Max(h.MinX, MathF.Min(p.X, h.MaxX));
                float cz = MathF.Max(h.MinZ, MathF.Min(p.Z, h.MaxZ));
                float dx = p.X - cx, dz = p.Z - cz;
                if(dx * dx + dz * dz < Level.PlayerRadius * Level.PlayerRadius)
                {
                    State = GameState.Lost;
                    return;
                }
            }
        }

       
        //check for win conditions
        float reach = _level.Objective.Radius + Level.PlayerRadius;
        foreach (var p in _players.Values)
        {
            //same as distance check in movement.js, this is checking collision math (sq distance vs radius)
            // reach = prize + player radius, so toucing it counts. if state==playing is a guard to stay won
            float dx = p.X - _level.Objective.X;
            float dz = p.Z - _level.Objective.Z;
            if(dx*dx + dz*dz < reach*reach)
            {
                State = GameState.Won;

                //Ai Code, Discard input that could be reused after Game Over
                //_inputs.Clear();  //redundant
                //End Ai Code

                break;
            }
        }
    }

    private bool HitsAny(float x, float z)
    {
        foreach (var w in _level.Walls)
            if (HitsBox(x, z, w.MinX, w.MaxX, w.MinZ, w.MaxZ)) return true;

        for (int d = 0; d < _doors.Count; d++)
            if (!_doorOpen[d])
            {
                var door = _doors[d];
                if (HitsBox(x, z, door.MinX, door.MaxX, door.MinZ, door.MaxZ)) return true;
            }
        return false;
    }

    private bool HitsBox(float x, float z, float minX, float maxX, float minZ, float maxZ)
    {
        float cx = MathF.Max(minX, MathF.Min(x, maxX));
        float cz = MathF.Max(minZ, MathF.Min(z, maxZ));
        float dx = x - cx, dz = z - cz;
        return dx * dx + dz * dz < Level.PlayerRadius * Level.PlayerRadius;
    }

    private bool TriggerSatisfied(TriggerData t)
    {
        int need = t.Requires <= 0 ? 1 : t.Requires;
        int count = 0;
        foreach(var p in _players.Values)
            if (p.X >= t.MinX && p.X <= t.MaxX && p.Z >= t.MinZ && p.Z <= t.MaxZ)
                count++;
        return count >= need;
    }
}

