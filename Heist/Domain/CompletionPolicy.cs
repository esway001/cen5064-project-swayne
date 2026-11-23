namespace Heist.Domain;

public static class CompletionPolicy
{
    //check if a player reached objective
    public static bool IsWon(IEnumerable<Player> players, ObjectiveData objective, float playerRadius)
    {
        float reach = objective.Radius + playerRadius;
        foreach (var p in players)
        {
            float dx = p.X - objective.X;
            float dz = p.Z - objective.Z;
            if (dx * dx + dz * dz < reach * reach) return true;
        }
        return false;
    }
}
