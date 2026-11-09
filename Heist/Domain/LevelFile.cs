namespace Heist.Domain;

public record LevelFile(
    int FormatVersion,
    IReadOnlyList<WallData> Walls,
    IReadOnlyList<HazardData> Hazards,
    IReadOnlyList<SpawnData> Spawns,
    ObjectiveData Objective,
    IReadOnlyList<DoorData>? Doors = null,
    IReadOnlyList<TriggerData>? Triggers = null);

public record WallData(string name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ);
public record HazardData(string name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ, float SafeMs, float ArmedMs);
public record SpawnData(string name, float X, float Z);
public record ObjectiveData(string name, float X, float Z, float Radius);
public record DoorData(string name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ, int CloseDelayMs);
public record TriggerData(string name, string Type, float MinX, float MaxX, float MinZ, float MaxZ, int Requires, string[] Targets);