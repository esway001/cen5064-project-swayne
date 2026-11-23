namespace Heist.Domain;

public static class TrapResolver
{
    public static void UpdateArmed(IReadOnlyList<HazardData> hazards, double elapsedMs, bool[] armed)
    {
        for (int i = 0; i < hazards.Count; i++)
        {
            var h = hazards[i];
            var cycle = h.SafeMs + h.ArmedMs;
            var t = elapsedMs % cycle;
            armed[i] = t >= h.SafeMs;          //safe then armed
        }
    }

    // is player touched Armed hazard? (closest point vs radius)
    public static bool HitsArmed(float x, float z, IReadOnlyList<HazardData> hazards, bool[] armed, float radius)
    {
        for (int i = 0; i < hazards.Count; i++)
        {
            if (!armed[i]) continue;
            var h = hazards[i];
            float cx = MathF.Max(h.MinX, MathF.Min(x, h.MaxX));
            float cz = MathF.Max(h.MinZ, MathF.Min(z, h.MaxZ));
            float dx = x - cx, dz = z - cz;
            if (dx * dx + dz * dz < radius * radius) return true;
        }
        return false;
    }
}
