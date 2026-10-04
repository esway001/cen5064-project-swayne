using Heist.Domain;
using Microsoft.AspNetCore.SignalR;

namespace Heist.Realtime; //fixed the namespace discrepancy
//SignalR Controller for handling connections, discconnecteions and messages.
public class GameHub: Hub
{
    private readonly RoomRegistry _registry;
    public GameHub(RoomRegistry registry) => _registry = registry;

    public async Task JoinRoom(string code, string name)
    {
        //SendAsync("eventName", data) sends msg to client with event name/data. Client listens for event and handles data accordingly.
        var room = _registry.GetOrCreate(code);
        var me = room.Add(Context.ConnectionId, name); //context.connectId is temp player id
        if (me is null)
        {
            await Clients.Caller.SendAsync("Rejected", "Room is full");
            return;
        }
        _registry.Track(Context.ConnectionId, code);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);   //This update putz conn into SignlR group by room code

        await Clients.Caller.SendAsync("Welcome", me, room.All); // Show current state to new player
        await Clients.OthersInGroup(code).SendAsync("PlayerJoined", me); // make accouncement to all
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var code = _registry.RemoveConnection(Context.ConnectionId);
        if (code is not null)
            await Clients.Group(code).SendAsync("PlayerLeft", Context.ConnectionId);
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
}
