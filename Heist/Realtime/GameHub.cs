using Heist.Domain;
using Microsoft.AspNetCore.SignalR;

namespace Heist.Realtime; //fixed the namespace discrepancy
//SignalR Controller for handling connections, disconnections and messages.
public class GameHub: Hub
{
    private readonly RoomRegistry _registry;
    private readonly LevelCatalog _levels;
    public GameHub(RoomRegistry registry, LevelCatalog levels){ _registry = registry; _levels = levels; }

    public IEnumerable<string> ListLevels() => _levels.Ids;
    public IEnumerable<RoomSummary> ListOpenRooms() =>
        _registry.ActiveRooms
            .Where(r => r.Status == RoomStatus.Waiting)
            .Select(r => new RoomSummary(r.Code, r.LevelId, r.PlayerCount))
            .ToArray();

    public async Task<string> CreateRoom(string name, string levelId)
    {
        if (!_levels.Has(levelId)) throw new HubException("Unknown level");
        var room = _registry.Create(levelId);
        room.HostConnId = Context.ConnectionId;         //make creator Host
        await JoinInternal(room, name);
        await BroadcastLobby();
        return room.Code;                               // caller shows on enter
    }

    public async Task JoinRoom(string code, string name)
    {
        var room = _registry.Get(code);
        if (room is null) throw new HubException("Room not found");     //can no longer conjure on join
        if (room.Status != RoomStatus.Waiting) throw new HubException("Game already started");
        await JoinInternal(room, name);
        await BroadcastLobby();                         //player count change
    }

    private async Task JoinInternal(Room room, string name)
    {
        var me = room.Add(Context.ConnectionId, name);
        if (me is null) throw new HubException("Room is full");
        _registry.Track(Context.ConnectionId, room.Code);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
        await Clients.Caller.SendAsync("Welcome", me, room.All, room.LevelId);  //in-game build
        await Clients.OthersInGroup(room.Code).SendAsync("PlayerJoined", me);   //in-game remote add
        await Clients.Group(room.Code).SendAsync("RosterChanged", room.All);    //waiting-room roster
    }

    public async Task StartGame()
    {
        var room = _registry.RoomOf(Context.ConnectionId);
        if (room is null || room.HostConnId != Context.ConnectionId) return;        //host only
        room.StartGame();
        await Clients.Group(room.Code).SendAsync("GameStarted");                    // once started, swap to game screen
        await BroadcastLobby();                                                     // room has left the lobby list
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var code = _registry.RemoveConnection(Context.ConnectionId);
        if (code is not null)
        {
            await Clients.Group(code).SendAsync("PlayerLeft", Context.ConnectionId);
            var room = _registry.Get(code);
            if (room is not null) await Clients.Group(code).SendAsync("RosterChanged", room.All);
            await BroadcastLobby();
        }
        //_registry.Remove(Context.ConnectionId);
        //await Clients.Others.SendAsync("PlayerLeft", Context.ConnectionId);
        await base.OnDisconnectedAsync(ex);
    }

    public void SendInput(InputCommand cmd)
    {
        _registry.RoomOf(Context.ConnectionId)?.SetInput(Context.ConnectionId, cmd);
        //For test purposes
        //Console.WriteLine($"input from {Context.ConnectionId[..4]}: seq={cmd.Seq} x={cmd.X} z={cmd.Z}");
    }

    private Task BroadcastLobby() => Clients.All.SendAsync("LobbyChanged");

    //public void StartGame()
    //{
    //    _registry.RoomOf(Context.ConnectionId)?.StartGame();
    //}
}
