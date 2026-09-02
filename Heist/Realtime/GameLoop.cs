using Microsoft.AspNetCore.SignalR;
using Heist.Domain;

namespace Heist.Hubs;

public class GameLoop : BackgroundService
{
    private readonly GameRegistry _registry;
    private readonly IHubContext<GameHub> _hub;

    //bridge between background loop and connected browsers. Loop is not inside hub instance where clients are.
    public GameLoop(GameRegistry registry, IHubContext<GameHub> hub)
    {
        _registry = registry;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        /*
         Fixed Clock heartbeat. Periodic Timer in seconds, but wakes every 50ms, runs one tick, sleeps again.
         Creates a steady rhythm to drive the simulation, this way an influx of messages doesn't create chaos 
         in the scene (if a stream of inputs come in at once due to erratic connection)
         */
        const float dt = 1f / 20f; //20 ticks per sec
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(dt));

        while (await timer.WaitForNextTickAsync(ct))
        {
            _registry.Step(dt);
            await _hub.Clients.All.SendAsync("Snapshot", _registry.All, ct);
        }
    }
}
