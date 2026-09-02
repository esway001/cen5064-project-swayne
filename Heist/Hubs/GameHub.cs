using Heist.Domain;
using Microsoft.AspNetCore.SignalR;

namespace Heist.Hubs;
//SignalR Controller for handling connections, discconnecteions and messages.
public class GameHub: Hub
{
    private readonly GameRegistry _registry;
    public GameHub(GameRegistry registry) => _registry = registry;

    public async Task Join(string name)
    {
        //SendAsync("eventName", data) sends msg to client with event name/data. Client listens for event and handles data accordingly.
        var me = _registry.Add(Context.ConnectionId, name); //context.connectId is temp player id
        if (me is null)
        {
            await Clients.Caller.SendAsync("Rejected", "Game is full");
            return;
        }
        await Clients.Caller.SendAsync("Welcome", me, _registry.All); // Show current state to new player
        await Clients.Others.SendAsync("PlayerJoined", me); // make accouncement to all
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        _registry.Remove(Context.ConnectionId);
        await Clients.Others.SendAsync("PlayerLeft", Context.ConnectionId);
        await base.OnDisconnectedAsync(ex);
    }
}
