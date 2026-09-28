namespace Heist.Domain;

//gonna use this for sending snapshots to clients, so they can update their local state
public record Snapshot(GameState State, IReadOnlyCollection<Player> Players);
