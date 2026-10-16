namespace Heist.Domain;

public record LevelFile(
    int FormatVersion,
    IReadOnlyList<WallData> Walls,
    IReadOnlyList<HazardData> Hazards,
    IReadOnlyList<SpawnData> Spawns,
    ObjectiveData Objective);

public record WallData(string name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ);
public record HazardData(string name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ, float SafeMs, float ArmedMs);
public record SpawnData(string name, float X, float Z);
public record ObjectiveData(string name, float X, float Z, float Radius);